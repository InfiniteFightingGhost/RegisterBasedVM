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
These raw pointers are stored inside a CPU-register-friendly `VMState` value-type struct. Index lookups are resolved using pointer arithmetic (e.g., `instPtr[state.Pc]`), which compiles to straight-line assembly with **zero branch checks**, allowing the CPU to execute instructions in a tight linear flow.

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
3. It fetches the second word from the instruction stream by advancing the PC: `secondInst = instPtr[++Pc];`.
4. It reads the comparison operator (`comp`) from the second instruction and performs the condition check:
   - `0` ($<$), `1` ($>$), `2` ($\le$), `3` ($\ge$).
5. **If condition is met (loop continues):**
   It updates the PC: `state.Pc += jumpOffset - 2` (jumping back to the start of the loop body).
6. **If condition is not met (loop exits):**
   The VM ignores the jump. The PC points to the second word (`PC+1`). At the end of the iteration, the main dispatch loop performs its standard `state.Pc++`, moving execution to `PC+2` (the instruction immediately following the `FOR` block).

This fuses three dispatches into **one single instruction cycle**, cutting loop control overhead in half.
