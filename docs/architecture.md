# VM Architecture & Calling Conventions

This document specifies the virtual machine's register layout, instruction encoding, parameter-passing conventions, and call stack representation.

## Bit-Packed Instruction Format

The VM uses fixed-width **32-bit instructions** represented by the `Instruction` struct. These instructions are bit-packed into four main layouts to accommodate various operations: `ABC`, `ABx`, `AsBx`, and `sBx26`.

### Bit Allocation Layouts

1. **ABC Format** (Three-register/constant operations like `ADD`, `SUB`, `MUL`, etc.):
   ```text
   +-----------------------+---------+---------+---------+
   |   C (9)   |   B (9)   |  A (8)  | Op (6)  | Bits
   +-----------------------+---------+---------+---------+
   31          22          13        5         0
   ```
   - **OpCode (6 bits):** Opcodes `0` to `63`.
   - **A (8 bits):** Destination register `0` to `255`.
   - **B (9 bits):** First operand. If `< 256`, maps to register. If `>= 256`, maps to constant pool index `B - 256`.
   - **C (9 bits):** Second operand. If `< 256`, maps to register. If `>= 256`, maps to constant pool index `C - 256`.

2. **ABx Format** (Two-operand operations with larger immediate constant pool indices like `LOADC`, `MOVE`, `CALL`, etc.):
   ```text
   +---------------------------------+---------+---------+
   |             Bx (18)             |  A (8)  | Op (6)  | Bits
   +---------------------------------+---------+---------+
   31                                13        5         0
   ```
   - **A (8 bits):** Destination register `0` to `255`.
   - **Bx (18 bits):** Unsigned index or immediate value (up to `262,143`).

3. **AsBx Format** (Two-operand operations with signed immediate offsets like `FOR` second-half):
   ```text
   +---------------------------------+---------+---------+
   |            sBx16 (16)           |  A (8)  | Op (6)  | Bits
   +---------------------------------+---------+---------+
   31                                13        5         0
   ```
   - **A (8 bits):** Opcode modifier/operand (e.g. comparison condition).
   - **sBx16 (16 bits):** Signed branch offset, biased by `32,767`.

4. **sBx26 Format** (Single-operand branch operations like `JUMP`):
   ```text
   +-------------------------------------------+---------+
   |                 sBx26 (26)                | Op (6)  | Bits
   +-------------------------------------------+---------+
   31                                          5         0
   ```
   - **sBx26 (26 bits):** Large signed branch offset, biased by `33,554,431`.

## Register/Constant Addressing (RC Operand Resolution)

To avoid separate instruction variants for register-register and register-constant operations (e.g., `ADD_RR` vs. `ADD_RC`), the VM utilizes a **Register/Constant (RC)** addressing mechanism.

For any 9-bit operand (fields `B` and `C` in `ABC` format, or `B` in `ABx` format when used as an operand):
- If the encoded value is **less than 256**, it references register `Registers[Index]` relative to active frame pointer `state.RegPtr`.
- If the encoded value is **greater than or equal to 256**, it references global constant `Constants[Index - 256]`.

This logic is implemented in [VirtualMachine.cs](../Raptor/VirtualMachine.cs) as follows:
```csharp
double valB = b < 256 ? Reg(state.RegPtr, b) : state.ConstPtr[b - 256];
```
This design maximizes code density, permitting up to 256 registers and 256 active constants to be accessed directly within a single three-address instruction.

## Sliding Register Windows (Zero-Copy Method Calls)

One of the VM's primary performance features is **sliding register windows**, which eliminates memory copies when passing arguments to methods.

### The Problem in Traditional VMs
In stack-based VMs or standard register VMs, calling a function requires copying arguments from the parent frame onto a call stack, or copying them into parameters registers for the callee. This copying wastes CPU cycles.

### The Zero-Copy Solution
The VM allocates a single continuous register file of 256 doubles on the thread stack. Instead of tracking an integer base pointer index, it shifts the active register file pointer (`state.RegPtr`) directly to keep access offsets zero-based in child frames.

When a method is called via `CALL A B`:
- `A` is the register index in the parent's frame where the callee's frame should start.
- `B` is the index of the callee method in the method table.

The VM pushes the current Program Counter index (calculated via `state.Ip - state.InstPtr`) and the current register pointer (`state.RegPtr`) onto the call stack, and then advances `state.RegPtr` directly to slide the window forward:
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
Because the callee's register frame begins exactly at `RegPtr_parent + A`, the parent can place arguments directly into registers starting at `R[A]` (relative to the parent). These arguments automatically become `R0`, `R1`, `R2`, etc. inside the callee, requiring **zero memory copies**.

### Returning Values
When returning via `RETURN A B` (returning values from callee's register range `[A, B]`):
1. The return values are copied from the returning range `[A, B]` to the start of the callee's frame (`R0, R1, ...`).
2. The VM pops the `StackFrame` off the stack and restores the parent's `RegPtr` and `Ip` (Instruction Pointer):
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

The call stack is represented by a contiguous block of `StackFrame` structures, allocated directly on the thread stack.

### StackFrame Structure
Each stack frame is a lightweight 16-byte structure tracking:
- `ReturnPC` (4 bytes): The program counter index (offset from the instructions array start) to jump back to in the parent.
- `PreviousRegPtr` (8 bytes): The parent's register pointer (pointing directly into the stackalloc registers block).

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

By keeping the call stack in stack-allocated memory (`stackalloc StackFrame[32]`), frame management operations compile to basic pointer dereferences and increments, bypassing C# heap allocation entirely.

## Bytecode Verification

To ensure safe sandboxed execution of untrusted user scripts, the compiled bytecode runs through a static analysis verifier (`BytecodeVerifier.cs`) before execution. The verifier performs three validation passes:

1. **Pass 1: Instruction Boundaries & Compound Scan:** Identifies the precise boundaries of all instructions. It scans the program to find two-word compound instructions (such as the `FOR` loop opcode pair) to mark invalid targets (e.g. the payload word), ensuring other branch instructions cannot jump into the middle of them.
2. **Pass 2: Operand & Control Flow Validation:** Validates instruction fields to guarantee:
   - All register indexes (`rA`, `rB`, `rC`) remain within the valid stack frame index limits (`[0, 255]`).
   - Constant pool indices are within range.
   - Jumps and branch targets (such as in `JUMP`, `FOR`, `CALL`, and conditional skips `EQ`/`LT`/`LE`) point strictly to valid instruction starts and do not escape the program array boundaries.
   - Memory allocator allocations (`NEWARR`) have non-negative size parameters.
3. **Pass 3: Terminal Control Check:** Asserts that the very last executable instruction in the bytecode chunk is a valid control flow terminator (`HALT`, `RETURN`, or `JUMP`), preventing the instruction pointer from executing past the end of the instruction block.

## The Assembler Pipeline (Overview)

The textual assembly code is compiled into execution chunks (`VMChunk`) using a modular **three-pass assembler**:
- **Pass 1 (Lexical & Macros):** Preprocesses strings, cleans whitespaces, strips comments, and expands symbol macros defined via the `DEFINE` syntax.
- **Pass 2 (Jump & Method Mapping):** Maps labels (`label:`) and methods (`method()`) to bytecode indices. To keep relative offsets aligned, it increments the PC counter by `2` when encountering a two-word `FOR` instruction, and `1` for all other instructions.
- **Pass 3 (Codegen):** Parses operands, performs constant pool allocation with deduplication, and packs instruction values into 32-bit words.

For a detailed specification of the compiler passes and constant table pooling, see the dedicated [Assembler Documentation](assembler.md).
