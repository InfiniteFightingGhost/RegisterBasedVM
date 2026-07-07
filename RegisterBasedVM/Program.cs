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
    ADD r1 r1 r2
    RETURN r1 r1";

string linearFib =
    @"DEFINE result r0
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
    LT 1 counter n
    JUMP loop
PRINT result
HALT";

string monteCarlo =
    @"
DEFINE epochs 1000000000
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
    LT 1 i epochs
    JUMP loop
DEFINE result r6
DIV result hits epochs
MUL result result 4
PRINT result
HALT";

//check if MUL SUB MUL SUB MUL SUB is faster than MUL MUL MUL SUB SUB SUB
string perceptron =
    @"
; --- CORE REGISTERS ---
DEFINE x1 r0
DEFINE x2 r1
DEFINE w1 r2
DEFINE w2 r3
DEFINE b r4
DEFINE expected r5
DEFINE result r6
DEFINE error r7
DEFINE epochs r8
DEFINE i r10

; --- SCRATCHPAD REGISTERS ---
DEFINE temp1 r21
DEFINE temp2 r22
DEFINE temp3 r23

; --- INITIALIZATION ---
LOADC epochs 10
LOADC w1 1
LOADC w2 1
LOADC b 0

loop:
    ; --- Example 1: (0, 0) -> 0 ---
    LOADC x1 0
    LOADC x2 0
    LOADC expected 0
    CALL error() x1
    CALL update() x1

    ; --- Example 2: (1, 0) -> 1 ---
    LOADC x1 1
    LOADC x2 0
    LOADC expected 0
    CALL error() x1
    CALL update() x1

    ; --- Example 3: (0, 1) -> 1 ---
    LOADC x1 0
    LOADC x2 1
    LOADC expected 0
    CALL error() x1
    CALL update() x1

    ; --- Example 4: (1, 1) -> 1 ---
    LOADC x1 1
    LOADC x2 1
    LOADC expected 1
    CALL error() x1
    CALL update() x1
    FOR i epochs 1 < loop

; --- OUTPUT RESULTS ---
PRINT w1
PRINT w2
PRINT b
HALT

; --- FUNCTIONS ---

print()
    PRINT w1
    PRINT w2
    PRINT b
    PRINTA 10
    RETURN r0 r0

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
    ; Update w1: w1 = w1 + (error * x1)
    MUL temp1 error x1
    ADD w1 w1 temp1

    ; Update w2: w2 = w2 + (error * x2)
    MUL temp2 error x2
    ADD w2 w2 temp2

    ; Update Bias: b = b + error
    ADD b b error
    
    RETURN r0 r0";

//FOR i epochs 1 < loop
VMChunk chunk = new VMChunk();
Assembler ass = new(chunk);
ass.Parse(perceptron.Split("\n").ToList());

VirtualMachine vm = new();
Console.WriteLine(chunk.Instructions.Count());
vm.LoadProgram(chunk, new int[] { });
vm.RunFast();
