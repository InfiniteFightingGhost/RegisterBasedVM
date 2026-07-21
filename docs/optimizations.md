# Performance & Hardware-Level Optimizations

Raptor uses low-level execution techniques to sustain >450 MIPS throughput in managed C#.

## Table of Contents
- [Managed Array Bounds Bypass via Pointers](#managed-array-bounds-bypass-via-pointers)
- [GCHandle Pinning & Stack Allocation](#gchandle-pinning-stack-allocation)
- [Switch Jump Table & Aggressive Inlining](#switch-jump-table-aggressive-inlining)
- [Xorshift32 PRNG Optimization](#xorshift32-prng-optimization)
- [Double-Precision Fast Inverse Square Root (FISR)](#double-precision-fast-inverse-square-root-fisr)
- [The Compound FOR Super-Instruction](#the-compound-for-super-instruction)
- [Zero-GC Output Formatting](#zero-gc-output-formatting)

## Managed Array Bounds Bypass via Pointers

To eliminate JIT bounds checking on array access, the VM pins managed arrays at initialization using `GCHandle.Alloc(..., Pinned)` and stores raw pointers as fields:

```csharp
_instHandle = GCHandle.Alloc(_instructions, GCHandleType.Pinned);
_constHandle = GCHandle.Alloc(_constants, GCHandleType.Pinned);
_methodsHandle = GCHandle.Alloc(_methods, GCHandleType.Pinned);
_heapHandle = GCHandle.Alloc(_heap, GCHandleType.Pinned);
_regHandle = GCHandle.Alloc(_registers, GCHandleType.Pinned);
```

Raw pointers are held in a `VMState` value-type struct. Opcodes are fetched by direct dereference and post-increment (e.g., `*state.Ip++`), eliminating lookup overhead.

## GCHandle Pinning & Stack Allocation

The 256-register file is heap-allocated and pinned via `GCHandle` at VM initialization, giving the interpreter a stable raw pointer for the VM's lifetime:

```csharp
private readonly double[] _registers = new double[256];
// pinned at init:
_regHandle = GCHandle.Alloc(_registers, GCHandleType.Pinned);
_regPtr = (double*)_regHandle.AddrOfPinnedObject();
```

Call stack frames are allocated on the thread execution stack using `stackalloc`:

```csharp
StackFrame* framePtr = stackalloc StackFrame[32];
```

Pinning the register array avoids per-frame GC allocations, while stack-allocating the call frames keeps frame push/pop overhead minimal (~4 cycles L1 latency vs. ~60 cycles for main memory).

## Switch Jump Table & Aggressive Inlining

Instruction handlers use aggressive inlining to eliminate call frame overhead:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static unsafe bool ExecuteAdd(Instruction instruction, ref VMState state) { ... }
```

Because the `OpCode` enum is densely packed, the JIT compiler emits a native jump table for the central dispatch switch.

## Xorshift32 PRNG Optimization

Random numbers are generated using a custom Xorshift32 algorithm stored as a `uint` (`state.RngState`) in thread state:

```csharp
state.RngState ^= state.RngState << 13;
state.RngState ^= state.RngState >> 17;
state.RngState ^= state.RngState << 5;
double result = state.RngState * 2.3283064365386963e-10;
```

Multiplying by `2.3283064365386963e-10` ($\frac{1}{2^{32}-1}$) scales the 32-bit state to a double in the range `[0.0, 1.0]` without integer division.

## Double-Precision Fast Inverse Square Root (FISR)

The `FISR` opcode computes $\frac{1}{\sqrt{x}}$ in 64-bit double precision using floating-point bit manipulation:

```csharp
x2 = valB * 0.5d;
y = valB;
i = *(long*)&y;                      // Reinterprets double precision floating point as 64-bit integer bits
i = 0x5fe6eb50c7b537a9 - (i >> 1);   // Double-precision magic constant
y = *(double*)&i;                    // Interpret long back as double
y = y * (threehalfs - (x2 * y * y)); // 1st Newton-Raphson iteration
y = y * (threehalfs - (x2 * y * y)); // 2nd Newton-Raphson iteration (for precision)
```

Bit shifting and subtracting from the double-precision constant `0x5fe6eb50c7b537a9` yields an initial guess accurate to 1.5%. Two Newton-Raphson iterations refine the result to full double precision.

> [!NOTE]
> *Implementation Note:* While modern CPUs feature dedicated hardware instructions for reciprocal square roots (e.g., `vsqrtpd` / `rsqrt`), software FISR provides a fallback software bit-manipulation algorithm within the VM opcode set.

## The Compound FOR Super-Instruction

The assembler compiles a `FOR` statement into two contiguous 32-bit instructions to reduce loop control dispatches:

```text
Instruction 1 (ABC Format):
+-----------------------+---------+---------+---------+
|     rStep (9 bits)    |  rMax   | rIndex  |   FOR   |
+-----------------------+---------+---------+---------+

Instruction 2 (AsBx Format):
+---------------------------------+---------+---------+
|       jumpOffset (16 bits)      |  comp   |   FOR   |
+---------------------------------+---------+---------+
```

Execution flow:
1. Fetch index, limit, and step values.
2. Increment index register: `valIndex += valStep;`.
3. Advance Instruction Pointer and fetch second word: `secondInst = new Instruction(*state.Ip++);`.
4. Check comparison operator (`comp`): `0` ($<$), `1` ($>$), `2` ($\le$), `3` ($\ge$).
5. **Condition met**: Update Instruction Pointer: `state.Ip += jumpOffset - 2` (branch back to loop body).
6. **Condition not met**: Execution continues past the second instruction word.

## Zero-GC Output Formatting

Output formatting writes directly into a stack/pinned buffer (`_outBuffer = new char[256]`):

1. `VMState` tracks buffer pointers and metrics: `char* OutBufferPtr`, `int OutBufferCapacity` (256), `int OutBufferOffset`.
2. Handlers (`ExecutePrint`, `ExecutePrintA`, `ExecutePrintS`) format doubles via `valB.TryFormat(span, out int charsWritten)`.
3. When remaining buffer capacity drops below 48 characters or a `HALT` instruction executes, the buffer flushes to standard output via `Console.Out.Write(new ReadOnlySpan<char>(state.OutBufferPtr, state.OutBufferOffset))` and resets `OutBufferOffset` to `0`.
