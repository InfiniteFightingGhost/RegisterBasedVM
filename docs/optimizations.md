# Performance & Hardware-Level Optimizations

To execute at speeds exceeding 450 MIPS in a managed environment, the VM utilizes low-level optimizations that align closely with CPU instruction pipelines and cache hierarchies.

---

## 1. Managed Array Bounds Bypass via Pointers

In normal C#, array access like `instructions[pc]` forces the JIT compiler to insert a boundary check:
```asm
cmp index, length
jae ThrowIndexOutOfRangeException
```
This check introduces conditional branches that pollute the CPU’s Branch Target Buffer (BTB) and cause instruction pipeline bubbles.

### The Pointer Fix
Before starting the hot execution loop, the VM pins all managed arrays using the `fixed` statement, obtaining raw pointers:
```csharp
fixed (uint* instPtr = _instructions)
fixed (double* constPtr = _constants)
fixed (uint* methodTablePtr = _methods)
fixed (byte* heapPtr = _heap)
```
These raw pointers are stored inside a CPU-register-friendly `VMState` value-type struct. Instruction dispatch fetches the next opcode by directly dereferencing and post-incrementing the instruction pointer pointer (e.g., `*state.Ip++`), which compiles to straight-line assembly with **zero branch checks** or array lookup overhead, allowing the CPU to execute instructions in a tight linear flow.

---

## 2. Stack Allocation & L1 Cache Locality

The VM's registers and call stack are allocated directly on the thread's local execution stack using `stackalloc`:
```csharp
double* RegPtr = stackalloc double[256];
StackFrame* framePtr = stackalloc StackFrame[32];
```
This yields two major benefits:
- **No GC Allocations:** Pointers are allocated on the stack and automatically cleaned up when the method exits. The garbage collector never runs during execution, avoiding GC pauses.
- **L1 Cache Locality:** Thread stacks are constantly read and written, keeping the 256-register file and call frames pinned in the CPU’s **L1 Data Cache** (access latency of ~4 cycles, compared to ~60 cycles for main memory).

---

## 3. Switch Jump Table & Aggressive Inlining

All instruction execution handlers are marked with `[MethodImpl(MethodImplOptions.AggressiveInlining)]`:
```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static unsafe bool ExecuteAdd(Instruction instruction, ref VMState state) { ... }
```
This forces the C# JIT compiler to eliminate the overhead of call frame setups and directly insert the handler code into the `switch` branches. 

Because the `OpCode` enum is densely packed, the JIT compiler compiles the central dispatch switch into a native **Jump Table** (a register-relative indirect jump array in assembly). The CPU’s branch predictor quickly learns this jump table pattern, reducing the dispatch overhead to a few clock cycles.

---

## 4. Xorshift32 PRNG Optimization

Standard random number generators like `System.Random` are slow, stateful classes that perform expensive divisions and heap lookups. 

For workloads like Monte Carlo simulations, the VM utilizes a custom **Xorshift32 Pseudo-Random Number Generator**. It maintains its state as a single `uint` in the thread state and computes the next random double using bitwise XORs and logical shifts:
```csharp
state.RngState ^= state.RngState << 13;
state.RngState ^= state.RngState >> 17;
state.RngState ^= state.RngState << 5;
double result = state.RngState * 2.3283064365386963e-10;
```
The floating-point multiplier `2.3283064365386963e-10` is exactly $\frac{1}{2^{32}-1}$. Multiplying by this constant scales the 32-bit integer state to a double in the range `[0.0, 1.0]` in a single clock cycle, avoiding slow divisions.

---

## 5. Double-Precision Fast Inverse Square Root (FISR)

The `FISR` instruction implements the classic Quake 3 Fast Inverse Square Root algorithm, but adapted for **64-bit double precision**.

It computes $\frac{1}{\sqrt{x}}$ without division or square-root hardware operations, using IEEE 754 bit-level hacking:
```csharp
x2 = valB * 0.5d;
y = valB;
i = *(long*)&y;                      // Evil bit-level hacking (interpret double as long)
i = 0x5fe6eb50c7b537a9 - (i >> 1);   // Double-precision magic constant
y = *(double*)&i;                    // Interpret long back as double
y = y * (threehalfs - (x2 * y * y)); // 1st Newton-Raphson iteration
y = y * (threehalfs - (x2 * y * y)); // 2nd Newton-Raphson iteration (for precision)
```
By performing a bit shift and subtracting from the double-precision magic number `0x5fe6eb50c7b537a9`, the VM obtains an initial guess accurate to 1.5%. Two iterations of Newton-Raphson refinement bring it to full double-precision accuracy, significantly faster than traditional division and square root.

---

## 6. The Compound `FOR` Super-Instruction

To speed up loops, the VM features a custom compound `FOR` super-instruction. 

### The Problem with Loop Overhead
A normal loop requires three VM instructions per iteration:
1. Increment index (`ADD`)
2. Compare index with limit (`LT` or `LE`)
3. Jump back to start if true (`JUMP`)
This requires **three separate instruction fetches and dispatches** at the end of every loop iteration.

### The Two-Word Instruction Solution
The assembler compiles a `FOR` statement into **two contiguous 32-bit instructions**:

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

### Execution Flow
When the VM dispatches the first `FOR` word:
1. It fetches the index, limit, and step values.
2. It increments the index register: `valIndex += valStep;`.
3. It fetches the second word from the instruction stream by directly dereferencing and advancing the Instruction Pointer: `secondInst = new Instruction(*state.Ip++);`.
4. It reads the comparison operator (`comp`) from the second instruction and performs the condition check:
   - `0` ($<$), `1` ($>$), `2` ($\le$), `3` ($\ge$).
5. **If condition is met (loop continues):**
   It updates the Instruction Pointer: `state.Ip += jumpOffset - 2` (jumping back to the start of the loop body).
6. **If condition is not met (loop exits):**
   The VM ignores the jump. Since the Instruction Pointer `state.Ip` was already advanced past the second word during the fetch on step 3, it naturally points to the instruction immediately following the `FOR` block, continuing linear execution.

This fuses three dispatches into **one single instruction cycle**, cutting loop control overhead in half.

---

## 7. Zero-GC Output Formatting (`OutBufferPtr`)

Standard output interpret functions like `Console.WriteLine` formatting `double` values directly to text generate heap allocation garbage (specifically C# string objects and formatting buffers). In a high-frequency execution environment (like a game scripting loop), output formatting garbage will quickly trigger GC collection spikes and frame rate drops.

To achieve complete zero-allocation output:
1. The virtual machine maintains a raw output buffer on the stack or pinned heap (`_outBuffer = new char[65536]`).
2. The `VMState` struct holds pointers and metrics for this buffer:
   - `char* OutBufferPtr`: Pointer to the start of the output buffer.
   - `int OutBufferCapacity`: Maximum character capacity of the buffer.
   - `int OutBufferOffset`: Current offset of written characters.
3. Instruction handlers like `ExecutePrint`, `ExecutePrintA`, and `ExecutePrintS` write directly to this buffer using `valB.TryFormat(span, out int charsWritten)` which formats the doubles into characters in-place without generating any C# string objects.
4. When the buffer is nearly full, or when the execution loop hits a `HALT` instruction, the buffer is flushed to standard output in a single batch operation using `Console.Out.Write(new ReadOnlySpan<char>(state.OutBufferPtr, state.OutBufferOffset))`, resetting the offset to `0`.

This output buffer mechanism ensures that even print-heavy workloads generate exactly **zero bytes of garbage** on the C# managed heap during interpretation.

---
