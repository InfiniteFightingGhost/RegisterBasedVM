using RegisterBasedVM;

string recursiveFib =
    @"DEFINE n 25
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
    RETURN r1 r1";

string linearFib =
    @"DEFINE result r0
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
HALT";

string monteCarlo =
    @"
DEFINE epochs 100000000
DEFINE x r1
DEFINE y r2
DEFINE hits r4
DEFINE i r5
loop:
    RAND x
    RAND y
    MUL x x x
    MUL y y y
    ADD y y x
    LE 0 y 1
    ADD hits hits 1
    ADD i i 1
    LE 0 i epochs
    JUMP loop
DEFINE result r6
DIV result hits epochs
MUL result result 4
PRINT result
HALT";

VMChunk chunk = new VMChunk();
Assembler ass = new(chunk);
ass.Parse(recursiveFib.Split("\n").ToList());

VirtualMachine vm = new();
Console.WriteLine(chunk.Instructions.Count());
vm.LoadProgram(chunk, new int[] { });
vm.RunFast();
