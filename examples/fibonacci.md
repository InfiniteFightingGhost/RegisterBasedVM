# Fibonacci Benchmark Examples

This document explores recursive and linear implementations of the Fibonacci sequence on the VM, detailing assembly code, instruction counts, and critical stack safety considerations.

---

## 1. Recursive Fibonacci (`recursiveFib`)

The recursive Fibonacci workload is highly function-call intensive. It stresses the VM's register window sliding, stack allocations, and frame push/pop performance.

### Assembly Code
```assembly
DEFINE n 25
DEFINE result r0
LOADC result n

CALL method() result
PRINT result
HALT

method()
    PRINT r0
    LE 1 r0 2
    JUMP math
    LOADC r0 1
    RETURN r0 r0

math:
    SUB r1 r0 1
    CALL method() r1
    SUB r2 r0 2
    CALL method() r2
    ADD r1 r1 r2
    RETURN r1 r1
```

### Analysis of the Function Call
- When `CALL method() result` runs, the register window slides by `0`
- `CALL method() r1` (line 21) -> slide window by 1.
  So `BasePtr` increments by 1. The parameter register for the callee (`r0`) corresponds to `r1` of the parent.
- When `CALL method() r2` (line 23) runs -> slide window by 2.
  So `BasePtr` increments by 2. The parameter register for the callee (`r0`) corresponds to `r2` of the parent.
- This creates overlapping register frames. Because we slide `BasePtr` by `1` or `2`, the arguments are already in place, achieving zero-copy calling.

### Stack safety analysis
The VM uses stack allocation for the call stack and a pinned heap array for registers:
- **Call Stack:** `stackalloc StackFrame[32]` (limit of 32 frames).
- **Register File:** `new double[256]` pinned via `GCHandle` (limit of 256 registers).

Because the recursive Fibonacci algorithm computes `Fib(N)` by recursively branching down to `Fib(1)`, the maximum depth of the call stack matches $N$.
- For $N = 25$, the maximum call depth is 25. This uses 25 stack frames and the register base pointer reaches 25, which is safely within limits.
- If $N \ge 32$, the execution exceeds the 32-frame `stackalloc` boundary. Because the VM has a call stack size check, if the call stack exceeds 32 stack frames the VM will throw a VMPanicException because of StackOverflow, allowing for method calls to be safe from memory corruption.

---

## 2. Linear Fibonacci (`linearFib`)

The linear Fibonacci implementation computes the sequence iteratively, avoiding call stack frames entirely. It serves as a benchmark for simple arithmetic loops.

### Assembly Code
```assembly
DEFINE result r0
DEFINE last r1
DEFINE lastlast r2
DEFINE counter r4
DEFINE n 5
LOADC result 1
LOADC counter 1
loop:
    MOVE lastlast last
    MOVE last result
    ADD result last lastlast
    ADD counter counter 1
    PRINT counter
    LT 0 counter n
    JUMP loop
PRINT result
HALT
```

### Analysis of the Iteration Loop
- In linear Fib, execution remains inside a single register frame.
- Operands like `last`, `lastlast`, and `result` are stored in consecutive registers `r1`, `r2`, and `r0`.
- The loop repeats until the condition `LT 1 counter n` fails. Because it bypasses function calls, it executes at maximum CPU speed (~470 MIPS).

---

## 3. Performance Summary

| Metric | Recursive Fibonacci ($N=25$) | Linear Fibonacci ($N=10^7$) |
| :--- | :--- | :--- |
| **Total VM Instructions** | ~242,785 | ~90,000,000 |
| **Absolute Execution Time** | ~0.7 ms | ~190 ms |
| **MIPS (Zen 4 Reference)** | ~340 MIPS | ~470 MIPS |
| **Call Stack Frames Used** | 25 (Peak) | 1 (Flat) |
| **Register Window Shift** | 25 (Peak) | 0 (Flat) |
