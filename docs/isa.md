# Instruction Set Architecture (ISA) Reference

This document serves as a complete reference manual for the virtual machine's Instruction Set Architecture (ISA).

---

## Operand Types Key

- **`rA`**: Destination register (8-bit index, `0` to `255`). Always resolves to a register relative to the active frame pointer `RegPtr`.
- **`rB` / `rC`**: Source register operands (9-bit index, `0` to `255`).
- **`kB` / `kC`**: Constant pool operands (9-bit index, encoded as `256` to `511`). Points to `Constants[Index - 256]`.
- **`opB` / `opC`**: Operands resolving via Register/Constant (RC) addressing. Can be a register (if $< 256$) or a constant (if $\ge 256$).
- **`Bx`**: Unsigned 18-bit immediate value or constant pool index.
- **`sBx16`**: Signed 16-bit branch offset, biased by `32,767`.
- **`sBx26`**: Signed 26-bit branch offset, biased by `33,554,431`.

---

## Instruction Set Table

| Opcode | Format | Syntax Example | Operation Description |
| :--- | :--- | :--- | :--- |
| **`LOADC`** | `ABx` | `LOADC rA Bx` | Loads constant from pool index `Bx` into register `rA`. <br> $\text{rA} = \text{Constants}[\text{Bx}]$ |
| **`MOVE`** | `ABx` | `MOVE rA rB` | Copies value from register `rB` (passed as lower 9 bits of `Bx`) into register `rA`. <br> $\text{rA} = \text{rB}$ |
| **`SWAP`** | `ABx` | `SWP rA rB` | Swaps the values of registers `rA` and `rB`. <br> $(\text{rA}, \text{rB}) = (\text{rB}, \text{rA})$ |
| **`ADD`** | `ABC` | `ADD rA opB opC` | Adds `opB` and `opC`, storing the result in `rA`. <br> $\text{rA} = \text{opB} + \text{opC}$ |
| **`SUB`** | `ABC` | `SUB rA opB opC` | Subtracts `opC` from `opB`, storing the result in `rA`. <br> $\text{rA} = \text{opB} - \text{opC}$ |
| **`MUL`** | `ABC` | `MUL rA opB opC` | Multiplies `opB` and `opC`, storing the result in `rA`. <br> $\text{rA} = \text{opB} \times \text{opC}$ |
| **`DIV`** | `ABC` | `DIV rA opB opC` | Divides `opB` by `opC`, storing the result in `rA`. <br> $\text{rA} = \text{opB} / \text{opC}$ |
| **`POW`** | `ABC` | `POW rA opB opC` | Computes `opB` to the power of `opC` using double precision. <br> $\text{rA} = \text{opB}^{\text{opC}}$ |
| **`UNM`** | `ABx` | `UNM rA opB` | Stores the unary negation of `opB` in `rA`. <br> $\text{rA} = -\text{opB}$ |
| **`MOD`** | `ABC` | `MOD rA opB opC` | Computes the floating-point modulo `opB % opC`, storing the result in `rA`. <br> $\text{rA} = \text{opB} \pmod{\text{opC}}$ |
| **`JUMP`** | `sBx26`| `JUMP label` | Performs relative jump. <br> $\text{Ip} += \text{sBx26} - 1$ |
| **`EQ`** | `ABC` | `EQ A opB opC` | Conditional skip. Compares `opB == opC`. If equality matches expected condition `(A != 0)`, the next instruction is skipped: <br> $\text{if } ((\text{opB} == \text{opC}) == (\text{A} \ne 0)) \implies \text{Ip}++$ |
| **`LT`** | `ABC` | `LT A opB opC` | Conditional skip. Compares `opB < opC`. If condition matches `(A != 0)`, the next instruction is skipped: <br> $\text{if } ((\text{opB} < \text{opC}) == (\text{A} \ne 0)) \implies \text{Ip}++$ |
| **`LE`** | `ABC` | `LE A opB opC` | Conditional skip. Compares `opB <= opC`. If condition matches `(A != 0)`, the next instruction is skipped: <br> $\text{if } ((\text{opB} \le \text{opC}) == (\text{A} \ne 0)) \implies \text{Ip}++$ |
| **`RAND`** | `ABC` | `RAND rA` | Generates a pseudo-random double in `[0, 1]` using XORShift32 and writes it to `rA`. |
| **`SQRT`** | `ABx` | `SQRT rA opB` | Computes the square root of `opB` and writes it to `rA`. <br> $\text{rA} = \sqrt{\text{opB}}$ |
| **`FISR`** | `ABx` | `FISR rA opB` | Computes the Fast Inverse Square Root of `opB` and writes it to `rA`. <br> $\text{rA} = 1 / \sqrt{\text{opB}}$ |
| **`CALL`** | `ABx` | `CALL method() rStart` | Slides register base pointer and transfers control. <br> $\text{StackFramePush}(\text{Ip} - \text{InstPtr}, \text{RegPtr})$ <br> $\text{RegPtr} += \text{rStart}$ <br> $\text{Ip} = \text{InstPtr} + \text{MethodTable}[\text{methodIndex}]$ |
| **`RETURN`** | `ABx` | `RETURN rStart rEnd` | Restores calling frame and returns values. Copies return range `[rStart, rEnd]` to callee window start, pops StackFrame, and restores parent `RegPtr` and `Ip`. |
| **`FOR`** | *2 Words*| `FOR rIndex max step < label` | **Word 1 (`ABC`):** Stores `rIndex`, `max`, `step`. <br> **Word 2 (`AsBx`):** Stores comparison operator (`comp`) and relative `jumpOffset`. <br> Increments `rIndex` by `step`. If condition `(rIndex comp max)` is true, jumps: $\text{Ip} += \text{jumpOffset} - 2$. Else, skips the second word: $\text{Ip}++$. |
| **`NEWARR`**| `ABC` | `NEWARR rA size` | Allocates a byte block of `size` bytes on the heap, and writes the resulting address to register `rA`. Note: `size` is raw bytes; double arrays require `elements * 8` bytes. |
| **`FREEARR`**| `ABC` | `FREEARR rA` | Deallocates the array pointed to by `rA` on the heap, and immediately coalesces contiguous free blocks. Sets `rA` to `0`. |
| **`SETARR`** | `ABC` | `SETARR rPtr index val` | Writes double `val` into heap array `rPtr` at element index `index` (offset in 8-byte steps). <br> $\text{Heap}[\text{rPtr} + \text{index} \times 8] = \text{val}$ |
| **`SETARRA`**| `ABC` | `SETARRA rPtr index val`| Writes byte `val` (ASCII character) into heap array `rPtr` at byte offset `index`. <br> $\text{Heap}[\text{rPtr} + \text{index}] = \text{val}$ |
| **`GETARR`** | `ABC` | `GETARR rA rPtr` | Reads double from heap array `rPtr` at element index `r0` (offset in 8-byte steps) and writes to register `rA`. <br> $\text{rA} = \text{Heap}[\text{rPtr} + \text{r0} \times 8]$ |
| **`GETARRA`**| `ABC` | `GETARRA rA rPtr`| Reads byte from heap array `rPtr` at byte offset `r0` and writes to register `rA`. <br> $\text{rA} = \text{Heap}[\text{rPtr} + \text{r0}]$ |
| **`PRINT`** | `ABC` | `PRINT opB` | Prints the double value of `opB` followed by a newline to the standard output. |
| **`PRINTA`**| `ABC` | `PRINTA opB` | Prints the value of `opB` cast to a character (ASCII) to the standard output. |
| **`HALT`** | `ABC` | `HALT` | Halts VM execution. Flushes and prints the contents of the thread's internal string buffer. |
| **`BINAND`**| `ABC` | `BINAND rA opB opC` | Performs bitwise AND of `opB` and `opC` (cast to `long`), writing the result as a double to `rA`. |
| **`BINOR`** | `ABC` | `BINOR rA opB opC`  | Performs bitwise OR of `opB` and `opC` (cast to `long`), writing the result as a double to `rA`. |
| **`BINXOR`**| `ABC` | `BINXOR rA opB opC` | Performs bitwise XOR of `opB` and `opC` (cast to `long`), writing the result as a double to `rA`. |
| **`BINLSH`**| `ABC` | `BINLSH rA opB opC` | Performs bitwise left shift of `opB` by `opC` bits (cast to integers), writing the result as a double to `rA`. |
| **`BINRSH`**| `ABC` | `BINRSH rA opB opC` | Performs bitwise right shift of `opB` by `opC` bits (cast to integers), writing the result as a double to `rA`. |

---

## Instruction Bit Encoding Helpers

For details on instruction parsing, bit masking, and layouts, see the [Core Architecture Specification](architecture.md).
