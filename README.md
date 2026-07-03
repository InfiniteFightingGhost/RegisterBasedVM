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
| EQ | A | B | C | if ((RC(B) == RC(C)) ~= A) then PC++ |
| LT | A | B | C | if ((RC(B) < RC(C)) ~= A) then PC++ |
| LE | A | B | C | if ((RC(B) <= RC(C)) ~= A) then PC++ |
| HALT | | | | |
| PRINT | A | | | Console.WriteLine(R(A)) |
| PRINTA | A | | | Console.WriteLine((char)R(A)) |

## Example programs:

### Fibonacci:
| Instruction Number   | Instruction    | Description |
|--------------- | --------------- | --- |
| 1   | LOADC 0 1    | Load 1 into R(0)|
| 2   | LOADC 3 50   | Load 50 into R(3). This tells the program how many numbers of Fibonacci it needs to compute |
| 3   | LOADC 4 1    | Load 1 into R(4). This is used to track on what Fibonacci we are currently|
| 4   | LOADC 5 1    | Load 1 into R(5). This is used in order to increment the index we hold in R(4)|
| 5   | PRINT 4      | Print the current Fibonacci number index we are on|
| 6   | PRINT 0      | Print the current Fibonacci number|
| 7   | MOVE 2 1     | Shift the value from R(1) to R(2)|
| 8   | MOVE 1 0     | Shift the value from R(0) to R(1)|
| 9   | ADD 0 1 2    | Add the values from R(1) and R(2) in order to get the current Fibonacci value|
| 10   | ADD 4 4 5    | Increment the index|
| 11   | LE 1 4 3     | Check if the index has reached the desired Fibonacci number. If it has it increments the PC by two, directly halting. If not, it increments only by one, which hits the jump instruction|
| 12   | JUMP -7      | Go back 7 instructions|
| 13   | HALT         | Stop the program|














