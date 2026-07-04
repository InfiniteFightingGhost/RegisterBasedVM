# RegisterBasedVM

This is a project that tries to build a register virtual machine, akin to the one that [Lua 5.0 implements](https://www.lua.org/doc/jucs05.pdf)
The speed it hit is 83.3 MIPS. This speed was achieved by doing the following things:
- Used a register based VM instead of a stack based one
- Used unmanaged function pointers for a O(1) dispatch table, bypassing the CPU's branch predictor
- Used unsafe code in order to eliminate bound checking overhead
## OpCodes:
Here is the notation I am using for ease of understanding:
 - R(n) -> nth register
 - C(n) -> nth value in the constants array
 - RC(n) -> either R(n) or C(n - k), depending on the value of n - it is R(n) for values smaller than k(default is 256)
 - PC -> program counter
 - CS -> Call stack

Each instruction is 32 bits long. The break down is:
Opcode is always 6 bits long

| Format | Destitation Register | RC 1 | RC 2 |
| --------------- | --------------- | --------------- | --------------- |
| ABC | 8 bits | 9 bits | 9 bits |
| ABx | 8 bits | 18 bits | 0 bits |
| sBx | 26 bits(signed) | 0 bits | 0 bits |


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
| CALL | sBx|  | | CS.Push(PC); PC += sBx,
| RET| | | | PC = CS.Pop() + 1 |
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
LOADC 0 1
LOADC 4 1
MOVE 2 1
MOVE 1 0
PRINT r0
ADD r0 r1 r2
ADD r4 r4 1
LT 0 r4 10
JUMP -6
PRINT r4
HALT
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


### Graveyard of raytraycers
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
