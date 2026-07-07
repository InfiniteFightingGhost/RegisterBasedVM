namespace RegisterBasedVM;

public enum OpCode
{
    LOADC, //0
    MOVE, // 1
    SWAP, // 2
    ADD, //  3
    SUB, //  4
    MUL, //  5
    DIV, // 6
    POW, // 7
    UNM, // 8
    JUMP, // 9
    EQ, // 10
    LT, // 11
    LE, // 12
    HALT, // 13
    PRINT, // 14
    PRINTA, // 15
    RAND, // 16
    SQRT, // 17
    FISR, // 18
    CALL, // 19
    RETURN, // 20
    FOR, // 21
    MOD, // 22
    NEWARR, // 23
    SETARR, // 24
    SETARRA, // 25
    GETARR, // 26
    GETARRA, // 27
}
