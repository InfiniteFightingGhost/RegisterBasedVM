# RegisterBasedVM

A high-performance(300 MIPS), Turing-complete virtual machine featuring SPARC-style overlapping register windows, stackless recursion, and a custom multi-pass assembler.
## Introduction and overview
The goal: Building a high performance register based VM to better understand how VM's work and how to write faster C# code
Core features:
 - Overlapping register window(Zero copy argument passing)
 - Custom 3 Pass Assembler(Labels, comments, DEFINE statements, forward JUMP resolution)
## The Assembler pipeline
The assembler is responsible for translating human-readable text into the raw 32-bit uint bytecode consumed by the Virtual Machine. To handle forward-jumping and macro expansion, the assembler operates in three distinct passes.
### The first pass:
The first pass is all about stripping everything away that is just syntactic sugar: comments and empty lines are all removed.
The assembler scans for DEFINE \[name\] \[value\] and replaces all subsequent instances of \[name\] with \[value\]

### The second pass:
The second pass focuses on labels and methods.
The assembler scans for lines ending with ":", checks if its not a JUMP and saves the label alongside the expected instruction index.
Methods are a little special, because their instruction index doesn't get saved in a dictionary, it gets saved in an array, and the index gets saved in the dictionary. This is done this way in order to allow for bit packing.

### The third pass:
This is the place where the magic happens.
The assembler iterates over the now clean instruction list and translates string opcodes(eg. ADD r1 r2 r3) into their binary representation.
This is also the place where the JUMP and CALL statements are resolved using the label and method dictionaries.

## OpCodes:
Here is the notation I am using for ease of understanding:
 - R(n) -> nth register
 - C(n) -> nth value in the constants array
 - RC(n) -> either R(n) or C(n - k), depending on the value of n - it is R(n) for values smaller than k(default is 256)
 - PC -> program counter
 - CS -> Call stack

### Bit packing
Each instruction is 32 bits long. The break down is:
Opcode is always 6 bits long
| Format | Destitation Register | RC 1 | RC 2 |
| --------------- | --------------- | --------------- | --------------- |
| ABC | 8 bits | 9 bits | 9 bits |
| ABx | 8 bits | 18 bits (unsigned) | 0 bits |
| sBx | 18 bits(signed) | 0 bits | 0 bits |

### Instruction Set Architecture(ISA)
| OpCode | Destitation Register | RC 1 | RC 2(if it's needed) | Notation |
| --------------- | --------------- | --------------- | --------------- | --------------- |
| LOADC | A | Bx | | R(A) := C(Bx) |
| MOVE | A | B | | R(A) := R(B) |
| SWP | A | B | | R(A) <=> R(B)
| ADD | A | B | C | R(A) := RC(B) + RC(C) |
| SUB | A | B | C | R(A) := RC(B) - RC(C) |
| MUL | A | B | C | R(A) := RC(B) * RC(C) |
| DIV | A | B | C | R(A) := RC(B) / RC(C) |
| POW | A | B | C | R(A) := RC(B) ^ RC(C) |
| UNM | A | B | | R(A) := -RC(B) |
| JUMP | sBx | | | PC += sBx |
| EQ | A | B | C | if ((RC(B) == RC(C)) != A) then PC++ |
| LT | A | B | C | if ((RC(B) < RC(C)) != A) then PC++ |
| LE | A | B | C | if ((RC(B) <= RC(C)) != A) then PC++ |
| HALT | | | | |
| PRINT | A | | | Console.WriteLine(R(A)) |
| PRINTA | A | | | Console.WriteLine((char)R(A)) |
| RAND | A | | | | R(A) = Random.NextSingle()
| SQRT | A | B | | R(A) := Math.Sqrt(RC(B)) |
| FISR | A | B | | R(A) := FISR algorithm (RC(B)) |
| CALL | A| B | | CS.Push(PC, Bias); PC = A, Bias += B
| RETURN| | | | (PC, Bias) = CS.Pop()|

## Example programs:

### Fibonacci:
| Instruction Number   | Instruction    | Description |
|--------------- | --------------- | --- |
| 1   | LOADC 0 1     | Load 1 into R(0)|
| 2   | LOADC 4 1     | Load 1 in R(4) for the counter|
| 3   | MOVE 2 1      | Shift the value from R(1) to R(2)|
| 4   | MOVE 1 0      | Shift the value from R(0) to R(1)|
| 5   | ADD r0 r1 r2  | Add the values from R(1) and R(2) in order to get the current Fibonacci value|
| 6   | ADD r4 r4 1   | Increment the index|
| 7   | LT 0 r4 10    | Check if the index has reached the desired Fibonacci number. If it has it increments the PC by two, directly halting. If not, it increments only by one, which hits the jump instruction|
| 8   | JUMP -6       | Jump the PC 6 places backward|
| 9   | PRINT r0       | Print the result|
| 10   | HALT           | Halt the program|
Code block for ease of ctrl + C:
```asm
DEFINE result r0
DEFINE last r1
DEFINE lastlast r2
DEFINE counter r4
DEFINE n 10
LOADC result 1
LOADC counter 1
loop:
    MOVE lastlast last
    MOVE last result
    ADD result last lastlast
    ADD counter counter 1
    LT 1 counter n
    JUMP loop
PRINT result
HALT
```

### Recursive Fibonacci
```asm
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
    PRINT r0
    PRINT r1
    PRINT r2
    ADD r1 r1 r2
    PRINT r1
    PRINTA 10
    RETURN r1 r1
```

### Monte Carlo PI approximator
| Instruction Number   | Instruction    | Description |
| --------------- | --------------- | --------------- |
| 1 | LOADC 0 10000000 | |
| 2 | RAND 1            | |
| 3 | RAND 2            | |
| 4 | POW r1 r1 2       | |
| 5 | POW r2 r2 2       | |
| 6 | ADD r2 r1 r2      | |
| 7 | LE 0 r2 1         | |
| 8 | ADD r4 r4 1       | |
| 9 | ADD r5 r5 1       | |
| 10 | LE 0 r5 r0        | |
| 11 | JUMP -10          | |
| 12 | DIV r6 r4 r0      | |
| 13 | MUL r6 r6 4       | |
| 14 | PRINT r6          | |
| 15 | HALT         | |


Code block for ease of ctrl + C:
```asm
LOADC 0 100000000
RAND 1
RAND 2
POW r1 r1 2
POW r2 r2 2
ADD r2 r1 r2
LE 0 r2 1
ADD r4 r4 1
ADD r5 r5 1
LE 0 r5 r0
JUMP -10
DIV r6 r4 r0
MUL r6 r6 4
PRINT r6
PRINT r4
PRINT r5
HALT
```

This piece of code runs in 1200~ ms on my machine, achieving the before-mentioned 83.3 MIPS

### Logic Gate Perceptron
Work in progress

### Mandlebrod set visualizer
Work in progress


### Graveyard of ray-tracers
```asm
LT 1 r1 40
JUMP 75
LT 1 r2 80
JUMP 69
DIV r3 r1 20
UNM r3 r3
ADD r3 r3 1
POW r4 r3 2
DIV r5 r2 40
SUB r5 r5 1
POW r6 r5 2
ADD r7 r4 r6
ADD r7 r7 1
FISR r8 r7
MUL r9 r5 r8
MUL r10 r3 r8
MUL r11 1 r8
MUL r8 r11 2
POW r12 r8 2
SUB r12 r12 3
SQRT r12 r12
SUB r13 r8 r12
MUL r14 r13 r9
MUL r15 r13 r10
MUL r16 r13 r11
SUB r16 r16 2
POW r17 r14 2
POW r18 r15 2
POW r19 r16 2
ADD r20 r17 r18
ADD r20 r20 r19
FISR r20 r20
MUL r15 r20 r15
ADD r15 r15 1
LE 1 r15 0.2
JUMP 2
PRINTA 32
JUMP 33
LE 1 r15 0.4
JUMP 2
PRINTA 46
JUMP 29
LE 1 r15 0.8
JUMP 2
PRINTA 58
JUMP 25
LE 1 r15 1.0
JUMP 2
PRINTA 45
JUMP 21
LE 1 r15 1.2
JUMP 2
PRINTA 61
JUMP 17
LE 1 r15 1.4
JUMP 2
PRINTA 42
JUMP 13
LE 1 r15 1.6
JUMP 2
PRINTA 42
JUMP 9
LE 1 r15 1.8
JUMP 2
PRINTA 35
JUMP 5
LE 1 r15 2.0
JUMP 2
PRINTA 64
JUMP 1
PRINTA 46
ADD r2 r2 1
JUMP -71
PRINTA 10
LOADC r2 0
ADD r1 r1 1
JUMP -77
HALT
```
