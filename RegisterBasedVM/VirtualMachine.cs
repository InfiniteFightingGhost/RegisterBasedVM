using System.Runtime.CompilerServices;

namespace RegisterBasedVM;

public class VirtualMachine
{
    private uint[] _instructions = null!;
    private float[] _constants = null!;
    int _pc = 0;
    private float[] _registers = new float[256];
    private static readonly Random random = new Random();

    public void LoadProgram(uint[] instructions, float[] constants)
    {
        _instructions = instructions;
        _constants = constants;
        _pc = 0;
        Array.Clear(_registers);
    }

    public void Run()
    {
        bool isRunning = true;

        while (isRunning)
        {
            uint instruction = _instructions[_pc];
            byte opcode = (byte)(instruction & 0x3F);
            byte a = (byte)((instruction >> 6) & 0xFF);
            // Console.WriteLine($"OpCode: {opcode}, destination: {a}");
            switch (opcode)
            {
                case (byte)OpCode.LOADC:
                    uint constantIndex = (byte)(instruction >> 14 & 0x0000FFFFFF);
                    _registers[a] = _constants[constantIndex];
                    break;
                case (byte)OpCode.MOVE:
                    byte b1 = (byte)((instruction >> 14) & 0xFF);
                    _registers[a] = _registers[b1];
                    break;
                case (byte)OpCode.SWP:
                    byte bSwap = (byte)((instruction >> 14) & 0xFF);
                    (_registers[a], _registers[bSwap]) = (_registers[bSwap], _registers[a]);
                    break;
                case (byte)OpCode.UNM:
                    byte b2 = (byte)((instruction >> 14) & 0xFF);
                    _registers[a] = -_registers[b2];
                    break;
                case (byte)OpCode.JUMP:
                    const int sBxBias = 33554431;
                    uint unsignedBx = (uint)(instruction >> 6);
                    int sBx = (int)(unsignedBx - sBxBias);
                    // Console.WriteLine(sBx);
                    _pc += sBx - 1;
                    break;
                case (byte)OpCode.ADD:
                    uint b3 = (uint)(instruction >> 14) & 0x1FF;
                    uint c3 = (uint)(instruction >> 23) & 0x1FF;
                    _registers[a] = _registers[b3] + _registers[c3];
                    break;
                case (byte)OpCode.SUB:
                    uint b4 = (uint)(instruction >> 14) & 0x1FF;
                    uint c4 = (uint)(instruction >> 23) & 0x1FF;
                    _registers[a] = _registers[b4] - _registers[c4];
                    break;
                case (byte)OpCode.MUL:
                    uint b5 = (uint)(instruction >> 14) & 0x1FF;
                    uint c5 = (uint)(instruction >> 23) & 0x1FF;
                    _registers[a] = _registers[b5] * _registers[c5];
                    break;
                case (byte)OpCode.DIV:
                    uint b6 = (uint)(instruction >> 14) & 0x1FF;
                    uint c6 = (uint)(instruction >> 23) & 0x1FF;
                    _registers[a] = _registers[b6] / _registers[c6];
                    break;
                case (byte)OpCode.POW:
                    uint b7 = (uint)(instruction >> 14) & 0x1FF;
                    uint c7 = (uint)(instruction >> 23) & 0x1FF;
                    _registers[a] = (float)Math.Pow(_registers[b7], _registers[c7]);
                    break;
                case (byte)OpCode.EQ:
                    uint b8 = (uint)(instruction >> 14) & 0x1FF;
                    uint c8 = (uint)(instruction >> 23) & 0x1FF;
                    bool comparison = _registers[b8] == _registers[c8];
                    bool expected = (a != 0);
                    if (comparison == expected)
                    {
                        _pc++;
                    }
                    break;
                case (byte)OpCode.LT:
                    uint b9 = (uint)(instruction >> 14) & 0x1FF;
                    uint c9 = (uint)(instruction >> 23) & 0x1FF;
                    bool comparison2 = _registers[b9] < _registers[c9];
                    bool expected2 = (a != 0);
                    if (comparison2 != expected2)
                    {
                        _pc++;
                    }
                    break;
                case (byte)OpCode.LE:
                    uint b10 = (uint)(instruction >> 14) & 0x1FF;
                    uint c10 = (uint)(instruction >> 23) & 0x1FF;
                    bool comparison3 = _registers[b10] <= _registers[c10];
                    bool expected3 = (a != 0);
                    if (comparison3 == expected3)
                    {
                        _pc++;
                    }
                    break;
                case (byte)OpCode.PRINT:
                    Console.WriteLine(_registers[a]);
                    break;
                case (byte)OpCode.PRINTA:
                    Console.Write((char)_registers[a]);
                    break;
                case (byte)OpCode.HALT:
                    isRunning = false;
                    break;
                default:
                    Console.WriteLine($"Uknown instruction at {_pc} -> {opcode}");
                    return;
            }
            _pc++;
        }
    }

    // The signature: Takes the instruction, memory pointers, and PC. Returns a bool.

    public unsafe void RunFast()
    {
        var dispatchTable = new delegate* <uint, float*, float*, ref int, bool>[64];
        dispatchTable[(int)OpCode.LOADC] = &ExecuteLoadC;
        dispatchTable[(int)OpCode.MOVE] = &ExecuteMove;
        // dispatchTable[(int)OpCode.SWP] = &ExecuteSwp;
        dispatchTable[(int)OpCode.ADD] = &ExecuteAdd;
        dispatchTable[(int)OpCode.SUB] = &ExecuteSub;
        dispatchTable[(int)OpCode.MUL] = &ExecuteMul;
        dispatchTable[(int)OpCode.DIV] = &ExecuteDiv;
        dispatchTable[(int)OpCode.POW] = &ExecutePow;
        dispatchTable[(int)OpCode.UNM] = &ExecuteUnm;
        dispatchTable[(int)OpCode.JUMP] = &ExecuteJump;
        dispatchTable[(int)OpCode.EQ] = &ExecuteEq;
        dispatchTable[(int)OpCode.LT] = &ExecuteLt;
        dispatchTable[(int)OpCode.LE] = &ExecuteLe;
        dispatchTable[(int)OpCode.HALT] = &ExecuteHalt;
        dispatchTable[(int)OpCode.PRINT] = &ExecutePrint;
        dispatchTable[(int)OpCode.PRINTA] = &ExecutePrintA;
        dispatchTable[(int)OpCode.RAND] = &ExecuteRand;
        fixed (uint* instPtr = _instructions)
        fixed (float* regPtr = _registers)
        fixed (float* constPtr = _constants)
        {
            bool isRunning = true;

            while (isRunning)
            {
                uint instruction = instPtr[_pc];
                byte opcode = (byte)(instruction & 0x3F);

                isRunning = dispatchTable[opcode](instruction, regPtr, constPtr, ref _pc);
                _pc++;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteLoadC(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint constantIndex = (byte)(instruction >> 14 & 0x0000FFFFFF);
        regPtr[a] = constPtr[constantIndex];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteMove(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        byte b = (byte)((instruction >> 14) & 0xFF);
        Console.WriteLine($"A: {a}, B: {b}");
        regPtr[a] = regPtr[b];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteUnm(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)((instruction >> 14) & 0xFF);
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        regPtr[a] = -valB;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteJump(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        const int sBxBias = 33554431;
        uint unsignedBx = (uint)(instruction >> 6);
        int sBx = (int)(unsignedBx - sBxBias);
        Console.WriteLine($"PC: {pc}, sbx: {sBx}");
        pc += sBx - 1;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteAdd(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        float valC = c < 256 ? regPtr[c] : constPtr[c - 256];
        regPtr[a] = valB + valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteSub(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        float valC = c < 256 ? regPtr[c] : constPtr[c - 256];
        regPtr[a] = valB - valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteMul(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        float valC = c < 256 ? regPtr[c] : constPtr[c - 256];
        regPtr[a] = valB * valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteDiv(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        float valC = c < 256 ? regPtr[c] : constPtr[c - 256];
        regPtr[a] = valB / valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecutePow(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        float valC = c < 256 ? regPtr[c] : constPtr[c - 256];
        regPtr[a] = (float)Math.Pow(valB, valC);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteEq(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        float valC = c < 256 ? regPtr[c] : constPtr[c - 256];
        bool comparison = valB == valC;
        bool expected = (a != 0);
        if (comparison == expected)
        {
            pc++;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteLt(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        float valC = c < 256 ? regPtr[c] : constPtr[c - 256];
        Console.WriteLine($"A:{a}, B: {valB} {b}, C: {valC}, {c}");
        bool comparison = valB < valC;
        bool expected = (a != 0);
        if (comparison != expected)
        {
            pc++;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteLe(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        float valC = c < 256 ? regPtr[c] : constPtr[c - 256];
        bool comparison = valB <= valC;
        bool expected = (a != 0);
        if (comparison == expected)
        {
            pc++;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecutePrint(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        uint a = (uint)((instruction >> 6) & 0xFF);
        float valA = a < 256 ? regPtr[a] : constPtr[a - 256];
        Console.WriteLine(valA);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecutePrintA(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        uint a = (uint)((instruction >> 6) & 0xFF);
        float valA = a < 256 ? regPtr[a] : constPtr[a - 256];
        Console.WriteLine((char)valA);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteHalt(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteRand(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        regPtr[a] = random.NextSingle();
        return true;
    }
}
