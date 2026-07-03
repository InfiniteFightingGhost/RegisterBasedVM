using RegisterBasedVM;

string instructions =
    @"LOADC 0 1
LOADC 4 1
MOVE 2 1
MOVE 1 0
PRINT r0
ADD r0 r1 r2
ADD r4 r4 1
LT 0 r4 10
JUMP -6
PRINT r4
HALT";

VMChunk chunk = new VMChunk();
Assembler ass = new(chunk);
DateTime start = DateTime.Now;
ass.Parse(instructions.Split("\n"));

VirtualMachine vm = new();

vm.LoadProgram(chunk.Instructions, chunk.Constants);
vm.RunFast();
DateTime end = DateTime.Now;
Console.WriteLine((end - start).Microseconds + " ms");
