# VM Architecture & Calling Conventions

This document specifies the virtual machine's register layout, instruction encoding, parameter-passing conventions, and call stack representation.

## Table of Contents
- [Bit-Packed Instruction Format](#bit-packed-instruction-format)
  - [Bit Allocation Layouts](#bit-allocation-layouts)
- [Register/Constant Addressing (RC Operand Resolution)](#registerconstant-addressing-rc-operand-resolution)
- [Sliding Register Windows](#sliding-register-windows)
  - [Argument Passing](#argument-passing)
  - [Returning Values](#returning-values)
- [The Call Stack](#the-call-stack)
  - [StackFrame Structure](#stackframe-structure)
- [Bytecode Verification](#bytecode-verification)
- [The Assembler Pipeline (Overview)](#the-assembler-pipeline-overview)

## Bit-Packed Instruction Format

The VM uses fixed-width 32-bit instructions represented by the `Instruction` struct. Instructions are packed into four layouts: `ABC`, `ABx`, `AsBx`, and `sBx26`.

### Bit Allocation Layouts

1. **ABC Format** (Three-register/constant operations like `ADD`, `SUB`, `MUL`):
   ```text
   +-----------------------+---------+---------+---------+
   |   C (9)   |   B (9)   |  A (8)  | Op (6)  | Bits
   +-----------------------+---------+---------+---------+
   31          22          13        5         0
   ```
   - OpCode (6 bits): Opcodes `0` to `63`.
   - A (8 bits): Destination register `0` to `255`.
   - B (9 bits): First operand. If `< 256`, maps to register. If `>= 256`, maps to constant pool index `B - 256`.
   - C (9 bits): Second operand. If `< 256`, maps to register. If `>= 256`, maps to constant pool index `C - 256`.

2. **ABx Format** (Two-operand operations with larger immediate constant pool indices like `LOADC`, `MOVE`, `CALL`):
   ```text
   +---------------------------------+---------+---------+
   |             Bx (18)             |  A (8)  | Op (6)  | Bits
   +---------------------------------+---------+---------+
   31                                13        5         0
   ```
   - A (8 bits): Destination register `0` to `255`.
   - Bx (18 bits): Unsigned index or immediate value (up to `262,143`).

3. **AsBx Format** (Two-operand operations with signed immediate offsets like `FOR` second-half):
   ```text
   +---------------------------------+---------+---------+
   |            sBx16 (16)           |  A (8)  | Op (6)  | Bits
   +---------------------------------+---------+---------+
   31                                13        5         0
   ```
   - A (8 bits): Opcode modifier/operand (e.g. comparison condition).
   - sBx16 (16 bits): Signed branch offset, biased by `32,767`.

4. **sBx26 Format** (Single-operand branch operations like `JUMP`):
   ```text
   +-------------------------------------------+---------+
   |                 sBx26 (26)                | Op (6)  | Bits
   +-------------------------------------------+---------+
   31                                          5         0
   ```
   - sBx26 (26 bits): Large signed branch offset, biased by `33,554,431`.

## Register/Constant Addressing (RC Operand Resolution)

The VM uses Register/Constant (RC) addressing for 9-bit operands (fields `B` and `C` in `ABC` format, or `B` in `ABx` format when used as an operand):
- If the value is less than 256, it references register `Registers[Index]` relative to active frame pointer `state.RegPtr`.
- If the value is 256 or greater, it references global constant `Constants[Index - 256]`.

Implementation in [VirtualMachine.cs](../Raptor/VirtualMachine.cs):
```csharp
double valB = b < 256 ? Reg(state.RegPtr, b) : state.ConstPtr[b - 256];
```
This permits up to 256 registers and 256 active constants to be accessed directly within a single three-address instruction.

## Sliding Register Windows

The VM allocates a single continuous register array of 256 doubles on the thread stack. Frame pointer `state.RegPtr` shifts directly when calling methods, keeping access offsets zero-based in child frames.

When calling a method via `CALL A B`:
- `A` is the register index in the parent's frame where the callee's frame starts.
- `B` is the index of the callee method in the method table.

The VM pushes the current Program Counter index (`state.Ip - state.InstPtr`) and register pointer (`state.RegPtr`) to the call stack, then advances `state.RegPtr`:
`RegPtr (callee) = RegPtr (parent) + A`

```csharp
byte frameStart = instruction.A;
int currentPcIndex = (int)(state.Ip - state.InstPtr);
StackFrame frame = new StackFrame(currentPcIndex, state.RegPtr);
CallStackPush(ref state.CallStackPtr, frame);
state.RegPtr += frameStart;
state.Ip = state.InstPtr + (int)vm._methods[methodIndex];
```

### Argument Passing
Because the callee register frame starts at `RegPtr (parent) + A`, arguments placed at `R[A]` by the parent are accessed as `R0`, `R1`, `R2`, etc. inside the callee without memory copies.

### Returning Values
When returning via `RETURN A B` (returning values from callee register range `[A, B]`):
1. Return values are copied from range `[A, B]` to the start of the callee frame (`R0, R1, ...`).
2. The VM pops `StackFrame` off the stack and restores parent `RegPtr` and `Ip`:
   `RegPtr (parent) = StackFrame.PreviousRegPtr`

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static unsafe bool ExecuteReturn(Instruction instruction, ref VMState state)
{
    byte start = instruction.A;
    byte end = (byte)instruction.B;
    byte count = (byte)(end - start);
    for (uint i = 0; i <= count; i++)
    {
        Reg(state.RegPtr, i) = Reg(state.RegPtr, start + i);
    }
    StackFrame frame = CallStackPop(ref state.CallStackPtr);
    state.RegPtr = frame.PreviousRegPtr;
    state.Ip = state.InstPtr + frame.ReturnPC;

    return true;
}
```

## The Call Stack

The call stack consists of contiguous `StackFrame` structures allocated on the thread stack via `stackalloc StackFrame[32]`.

### StackFrame Structure
Each 16-byte `StackFrame` tracks:
- `ReturnPC` (4 bytes): Instruction index offset from instructions array start to resume execution in parent.
- `PreviousRegPtr` (8 bytes): Parent register pointer in the stack-allocated registers block.

```csharp
public readonly unsafe struct StackFrame
{
    public readonly int ReturnPC;
    public readonly double* PreviousRegPtr;
    
    public StackFrame(int returnPC, double* previousRegPtr)
    {
        ReturnPC = returnPC;
        PreviousRegPtr = previousRegPtr;
    }
}
```

## Bytecode Verification

Before execution, `BytecodeVerifier.cs` validates compiled bytecode in three passes:

1. **Pass 1: Instruction Boundaries & Compound Scan:** Identifies instruction boundaries. Scans for two-word instructions (such as `FOR`) and marks payload words as invalid jump targets.
2. **Pass 2: Operand & Control Flow Validation:** Validates operand constraints:
   - Register indices (`rA`, `rB`, `rC`) are within `[0, 255]`.
   - Constant pool indices are within range.
   - Jump targets (`JUMP`, `FOR`, `CALL`, `EQ`, `LT`, `LE`) point to instruction boundaries within program bounds.
   - Array allocation (`NEWARR`) size parameters are non-negative.
3. **Pass 3: Terminal Control Check:** Confirms the final instruction is a valid terminator (`HALT`, `RETURN`, or `JUMP`).

## The Assembler Pipeline (Overview)

The assembly compiler uses a three-pass pipeline:
- **Pass 1 (Lexical & Macros):** Preprocesses text, strips comments, and expands `DEFINE` symbol macros.
- **Pass 2 (Jump & Method Mapping):** Maps labels (`label:`) and methods (`method()`) to bytecode indices, incrementing PC by 2 for `FOR` instructions and 1 for others.
- **Pass 3 (Codegen):** Parses operands, deduplicates constant pool entries, and packs 32-bit instructions.

See [Assembler Documentation](assembler.md) for full details.

