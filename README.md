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
