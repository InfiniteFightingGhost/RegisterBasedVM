# RegisterBasedVM

This is a project that tries to build a register virtual machine, akin to the one that [Lua 5.0 implements](https://www.lua.org/doc/jucs05.pdf)
I am heavily influenced by the Register VM of Lua 5.0, so keep that in mind
## OpCodes:
Here is the notation I am using for ease of understanding:
 - R(n) -> nth register
 - C(n) -> nth value in the constants array
 - RC(n) -> either R(n) or C(n - k), depending on the value of n - it is R(n) for values smaller than k(default is 256)
 - PC -> program counter

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














