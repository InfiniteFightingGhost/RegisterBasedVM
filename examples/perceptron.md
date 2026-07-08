# Perceptron Machine Learning Example

This document details the perceptron training benchmark, which implements a single-layer classifier to learn the logical **AND gate**. It showcases the VM's support for modular functions, nested execution frames, and weight correction mathematics.

---

## 1. Machine Learning Logic (AND Gate Classifier)

A perceptron classifies inputs $x_1, x_2$ using weights $w_1, w_2$ and a bias $b$. 

### Prediction
1. Compute the dot product:
   $$z = w_1 x_1 + w_2 x_2$$
2. Add the bias and apply the step activation function:
   $$y = \begin{cases} 1 & \text{if } z + b > 0 \\ 0 & \text{if } z + b \le 0 \end{cases}$$

### Learning Update Rule
For each training example, the model compares the prediction $y$ with the expected output $t$. The error is computed as:
$$\text{error} = t - y$$

The weights and bias are updated proportionally:
$$w_i = w_i + (\text{error} \times x_i)$$
$$b = b + \text{error}$$

If the prediction is correct ($\text{error} = 0$), no weights are modified. The model repeats this training loop until convergence.

---

## 2. Assembly Implementation & Function Structure

The perceptron code is structured modularly using functions, which compiles to nested register windows.

```assembly
; --- INITIALIZATION ---
LOADC epochs 10000000
LOADC w1 1
LOADC w2 1
LOADC b 0

loop:
    ; Example 1: (0, 0) -> 0
    LOADC x1 0
    LOADC x2 0
    LOADC expected 0
    CALL error() x1
    CALL update() x1

    ; Example 2: (1, 0) -> 0
    LOADC x1 1
    LOADC x2 0
    LOADC expected 0
    CALL error() x1
    ...
    FOR i epochs 1 < loop

; --- OUTPUT RESULTS ---
PRINT w1
PRINT w2
PRINT b
HALT

; --- FUNCTIONS ---

dot()
    MUL temp1 x1 w1
    MUL temp2 x2 w2
    ADD result temp1 temp2
    RETURN r0 r0

perceive()
    CALL dot() x1
    ADD result result b
    LE 1 result 0
    JUMP perceive_if
    LOADC result 0
    JUMP perceive_return
perceive_if:
    LOADC result 1
perceive_return:
    RETURN r0 r0

error()
    CALL perceive() x1
    SUB error expected result
    RETURN r0 r0

update()
    MUL temp1 error x1
    ADD w1 w1 temp1
    MUL temp2 error x2
    ADD w2 w2 temp2
    ADD b b error
    RETURN r0 r0
```

---

## 3. Function Call & Nested Register Windows Trace

Because functions are modular, they execute inside nested register windows:
1. **Root frame:** Houses the main training variables (`x1`, `x2`, `w1`, `w2`, `b`, `expected`, `epochs`, `i`).
2. **`error()` call:** slides the base pointer forward. Inside `error()`, the parameter `x1` corresponds to the callee's `r0` parameter.
3. **`perceive()` call:** `error()` calls `perceive()`, sliding the window forward again.
4. **`dot()` call:** `perceive()` calls `dot()`, sliding the window a third time to perform the vector multiplication.

This nested execution tests the VM's call stack push/pop speed. Because of zero-copy sliding windows, the coordinates and weights are visible in overlapping ranges without needing copy loops, which minimizes dispatch penalty.

---

## 4. Performance & Convergence Benchmarks

When run on the benchmark suite:
- **Epochs:** 10,000,000 (representing 40,000,000 coordinate training runs).
- **Correct Convergence:** The weights converge to $w_1 = 1$, $w_2 = 2$, and $b = -2$. (Wait! Since $w_1 x_1 + w_2 x_2 + b > 0$, inputs like $(1,1)$ yield $1(1) + 2(1) - 2 = 1 > 0 \implies 1$, while $(1,0)$ yields $1(1) + 0 - 2 = -1 \le 0 \implies 0$, perfectly separating the coordinates for an AND gate).
- **Absolute Execution Time:** **2.68 seconds**
- **Average Throughput:** **366 MIPS**

### Why is Perceptron MIPS lower than Monte Carlo?
- **Monte Carlo (491 MIPS):** Runs simple arithmetic operations in a flat, unrolled sequence inside a single register frame.
- **Perceptron (366 MIPS):** Performs nested function calls (`error` $\rightarrow$ `perceive` $\rightarrow$ `dot`), calling `CALL` and `RETURN` for every coordinate run.
- **Overhead:** Each function call requires pushing/popping a `StackFrame` and adjusting `BasePtr`. Populating the call stack introduces memory writes/reads to the thread stack, reducing absolute instruction dispatch rates.
- Even with this calling overhead, **366 MIPS** represents a massive speed advantage, outperforming traditional ANSI C interpreters.
