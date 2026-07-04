using System.Runtime.CompilerServices;

namespace RegisterBasedVM;

public unsafe class VirtualMachine
{
    private uint[] _instructions = null!;
    private float[] _constants = null!;
    int _pc = 0;
    private float[] _registers = new float[256];
    private static readonly Random random = new Random();
    private int[] _breakpoints = null!;
    private int[] _callStack = new int[16];

    public void LoadProgram(uint[] instructions, float[] constants, int[] breakpoints)
    {
        _instructions = instructions;
        _constants = constants;
        _pc = 0;
        Array.Clear(_registers);
        _breakpoints = breakpoints;
    }

    private void DumpRegisters(int count = 32)
    {
        for (int i = 0; i < count; i++)
        {
            int bits = BitConverter.SingleToInt32Bits(_registers[i]);
            Console.Write($"R{i:D2}: 0x{bits:X8} | ");

            if ((i + 1) % 4 == 0)
                Console.WriteLine();
        }
    }

    public unsafe void RunFast()
    {
        var dispatchTable = new delegate* <uint, float*, float*, ref int, int*, bool>[64];
        dispatchTable[(int)OpCode.LOADC] = &ExecuteLoadC;
        dispatchTable[(int)OpCode.MOVE] = &ExecuteMove;
        dispatchTable[(int)OpCode.SWP] = &ExecuteSwp;
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
        dispatchTable[(int)OpCode.FISR] = &ExecuteFisr;
        dispatchTable[(int)OpCode.SQRT] = &ExecuteSqrt;
        dispatchTable[(int)OpCode.CALL] = &ExecuteFisr;
        dispatchTable[(int)OpCode.RET] = &ExecuteSqrt;
        fixed (uint* instPtr = _instructions)
        fixed (float* regPtr = _registers)
        fixed (float* constPtr = _constants)
        fixed (int* callStackPtr = _callStack)
        {
            bool isRunning = true;
            Console.WriteLine("Starting VM...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (isRunning)
            {
                if (_breakpoints.Contains(_pc))
                {
                    DumpRegisters();
                    Console.ReadLine();
                }
                uint instruction = instPtr[_pc];
                byte opcode = (byte)(instruction & 0x3F);
                isRunning = dispatchTable[opcode]
                    (instruction, regPtr, constPtr, ref _pc, callStackPtr);
                _pc++;
            }
            stopwatch.Stop();
            Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteLoadC(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc,
        int* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint constantIndex = (byte)(instruction >> 14 & 0xFFFFFF);
        regPtr[a] = constPtr[constantIndex];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteMove(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc,
        int* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        byte b = (byte)((instruction >> 14) & 0xFF);
        regPtr[a] = regPtr[b];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteSwp(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc,
        int* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        byte b = (byte)((instruction >> 14) & 0xFF);
        (regPtr[a], regPtr[b]) = (regPtr[b], regPtr[a]);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteUnm(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc,
        int* callStackPtr
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
        ref int pc,
        int* callStackPtr
    )
    {
        const int sBxBias = 33554431;
        uint unsignedBx = (uint)(instruction >> 6);
        int sBx = (int)(unsignedBx - sBxBias);
        pc += sBx - 1;
        return true;
    }

    public static unsafe bool ExecuteCall(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc,
        int* callStackPtr
    )
    {
        const int sBxBias = 33554431;
        uint unsignedBx = (uint)(instruction >> 6);
        int sBx = (int)(unsignedBx - sBxBias);
        *(callStackPtr++) = pc;
        pc += sBx - 1;
        return true;
    }

    public static unsafe bool ExecuteRet(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc,
        int* callStackPtr
    )
    {
        int target = *(callStackPtr--);
        pc = target;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteAdd(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc,
        int* callStackPtr
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
        ref int pc,
        int* callStackPtr
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
        ref int pc,
        int* callStackPtr
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
        ref int pc,
        int* callStackPtr
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
        ref int pc,
        int* callStackPtr
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
        ref int pc,
        int* callStackPtr
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
        ref int pc,
        int* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        float valC = c < 256 ? regPtr[c] : constPtr[c - 256];
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
        ref int pc,
        int* callStackPtr
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
        ref int pc,
        int* callStackPtr
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
        ref int pc,
        int* callStackPtr
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
        ref int pc,
        int* callStackPtr
    )
    {
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteRand(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc,
        int* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        regPtr[a] = random.NextSingle();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteSqrt(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc,
        int* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)((instruction >> 14) & 0xFF);
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        regPtr[a] = (float)Math.Sqrt(valB);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteFisr(
        uint instruction,
        float* regPtr,
        float* constPtr,
        ref int pc,
        int* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)((instruction >> 14) & 0xFF);
        float valB = b < 256 ? regPtr[b] : constPtr[b - 256];
        long i;
        float x2,
            y;
        const float threehalfs = 1.5F;

        x2 = valB * 0.5F;
        y = valB;
        i = *(int*)&y; // evil floating point bit level hacking
        i = 0x5f3759df - (i >> 1); // what the fuck?
        y = *(float*)&i;
        y = y * (threehalfs - (x2 * y * y)); // 1st iteration
        //	y  = y * ( threehalfs - ( x2 * y * y ) );   // 2nd iteration, this can be removed
        regPtr[a] = y;
        return true;
    }
}
