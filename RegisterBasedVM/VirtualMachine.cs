using System.Runtime.CompilerServices;

namespace RegisterBasedVM;

public unsafe class VirtualMachine
{
    private uint[] _instructions = null!;
    private double[] _constants = null!;
    int _pc = 0;
    int _basePtr = 0;
    private static readonly Random random = new Random();
    private int[] _breakpoints = null!;
    private uint[] _methods = null!;

    public void LoadProgram(VMChunk chunk, int[] breakpoints)
    {
        _instructions = chunk.Instructions;
        _constants = chunk.Constants;
        _methods = chunk.MethodTable;
        _pc = 0;
        _basePtr = 0;
        _breakpoints = breakpoints;
    }

    private unsafe void DumpRegisters(double* registers, int count = 32)
    {
        for (int i = 0; i < count; i++)
        {
            Console.Write($"R{i:D2}: {registers[i]:G} | ");

            if ((i + 1) % 2 == 0)
                Console.WriteLine();
        }
    }

    public unsafe void RunFast()
    {
        delegate* <
            uint,
            double*,
            double*,
            uint*,
            ref int,
            ref int,
            ref StackFrame*,
            bool>* dispatchTable =
            stackalloc delegate* <
                uint,
                double*,
                double*,
                uint*,
                ref int,
                ref int,
                ref StackFrame*,
                bool>[64];

        dispatchTable[(int)OpCode.LOADC] = &ExecuteLoadC;
        dispatchTable[(int)OpCode.MOVE] = &ExecuteMove;
        dispatchTable[(int)OpCode.SWAP] = &ExecuteSwp;
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
        dispatchTable[(int)OpCode.CALL] = &ExecuteCall;
        dispatchTable[(int)OpCode.RETURN] = &ExecuteReturn;

        double* regPtr = stackalloc double[256];
        Unsafe.InitBlockUnaligned(regPtr, 0, 256 * sizeof(float));

        StackFrame* framePtr = stackalloc StackFrame[32];
        fixed (uint* instPtr = _instructions)
        fixed (double* constPtr = _constants)
        fixed (uint* methodTablePtr = _methods)
        {
            bool isRunning = true;
            Console.WriteLine("Starting VM...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (isRunning)
            {
#if DEBUG
                if (_breakpoints.Contains(_pc))
                {
                    DumpRegisters(regPtr);
                    Console.ReadLine();
                }
#endif
                uint instruction = instPtr[_pc];
                byte opcode = (byte)(instruction & 0x3F);
                isRunning = dispatchTable[opcode]
                    (
                        instruction,
                        regPtr,
                        constPtr,
                        methodTablePtr,
                        ref _pc,
                        ref _basePtr,
                        ref framePtr
                    );
                _pc++;
            }
            stopwatch.Stop();
            Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CallStackPush(ref StackFrame* stackFramePtr, StackFrame frame)
    {
        *stackFramePtr = frame;
        stackFramePtr++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StackFrame CallStackPop(ref StackFrame* stackFramePtr)
    {
        stackFramePtr--;
        StackFrame frame = *stackFramePtr;
        return frame;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ref double Reg(double* regPtr, int basePtr, uint index)
    {
        return ref regPtr[basePtr + index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteLoadC(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint constantIndex = (byte)(instruction >> 14 & 0xFFFFFF);
        Reg(regPtr, basePtr, a) = constPtr[constantIndex];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteMove(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        byte b = (byte)((instruction >> 14) & 0xFF);
        Reg(regPtr, basePtr, a) = Reg(regPtr, basePtr, b);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteSwp(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        byte b = (byte)((instruction >> 14) & 0xFF);
        (Reg(regPtr, basePtr, a), Reg(regPtr, basePtr, b)) = (
            Reg(regPtr, basePtr, b),
            Reg(regPtr, basePtr, a)
        );
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteUnm(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)((instruction >> 14) & 0xFF);
        double valB = b < 256 ? Reg(regPtr, basePtr, b) : constPtr[b - 256];
        Reg(regPtr, basePtr, a) = -valB;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteJump(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        const int sBxBias = 33554431;
        uint unsignedBx = (uint)(instruction >> 6);
        int sBx = (int)(unsignedBx - sBxBias);
        pc += sBx - 1;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteCall(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        ushort methodIndex = (ushort)((instruction >> 6) & 0x1FF);
        int start = (int)((instruction >> 15));
        StackFrame frame = new StackFrame(pc, basePtr);
        CallStackPush(ref callStackPtr, frame);
        basePtr += start;

        pc = (int)methodTablePtr[methodIndex];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteReturn(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte start = (byte)((instruction >> 6) & 0xFF);
        byte end = (byte)(instruction >> 14);
        byte count = (byte)(end - start);
        for (uint i = 0; i <= count; i++)
        {
            Reg(regPtr, basePtr, i) = Reg(regPtr, basePtr, start + i);
        }
        StackFrame frame = CallStackPop(ref callStackPtr);
        int target = frame.ReturnPC;
        basePtr = frame.PreviousBase;
        pc = target;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteAdd(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        double valB = b < 256 ? Reg(regPtr, basePtr, b) : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        double valC = c < 256 ? Reg(regPtr, basePtr, c) : constPtr[c - 256];
        Reg(regPtr, basePtr, a) = valB + valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteSub(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        double valB = b < 256 ? Reg(regPtr, basePtr, b) : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        double valC = c < 256 ? Reg(regPtr, basePtr, c) : constPtr[c - 256];
        Reg(regPtr, basePtr, a) = valB - valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteMul(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        double valB = b < 256 ? Reg(regPtr, basePtr, b) : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        double valC = c < 256 ? Reg(regPtr, basePtr, c) : constPtr[c - 256];
        Reg(regPtr, basePtr, a) = valB * valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteDiv(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        double valB = b < 256 ? Reg(regPtr, basePtr, b) : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        double valC = c < 256 ? Reg(regPtr, basePtr, c) : constPtr[c - 256];
        Reg(regPtr, basePtr, a) = valB / valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecutePow(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        double valB = b < 256 ? Reg(regPtr, basePtr, b) : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        double valC = c < 256 ? Reg(regPtr, basePtr, c) : constPtr[c - 256];
        Reg(regPtr, basePtr, a) = (float)Math.Pow(valB, valC);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteEq(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        double valB = b < 256 ? Reg(regPtr, basePtr, b) : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        double valC = c < 256 ? Reg(regPtr, basePtr, c) : constPtr[c - 256];
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
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)(instruction >> 14) & 0x1FF;
        double valB = b < 256 ? Reg(regPtr, basePtr, b) : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        double valC = c < 256 ? Reg(regPtr, basePtr, c) : constPtr[c - 256];
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
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)((instruction >> 14) & 0x1FF);
        double valB = b < 256 ? Reg(regPtr, basePtr, b) : constPtr[b - 256];
        uint c = (uint)(instruction >> 23) & 0x1FF;
        double valC = c < 256 ? Reg(regPtr, basePtr, c) : constPtr[c - 256];
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
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        uint a = (uint)((instruction >> 6) & 0xFF);
        double valA = a < 256 ? Reg(regPtr, basePtr, a) : constPtr[a - 256];
        Console.WriteLine(valA);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecutePrintA(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        uint a = (uint)((instruction >> 6) & 0x1FF);
        double valA = a < 256 ? Reg(regPtr, basePtr, a) : constPtr[a - 256];
        Console.Write((char)valA);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteHalt(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteRand(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        Reg(regPtr, basePtr, a) = random.NextSingle();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteSqrt(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    )
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)((instruction >> 14) & 0xFF);
        double valB = b < 256 ? Reg(regPtr, basePtr, b) : constPtr[b - 256];
        Reg(regPtr, basePtr, a) = (float)Math.Sqrt(valB);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteFisr(
        uint instruction,
        double* regPtr,
        double* constPtr,
        uint* methodTablePtr,
        ref int pc,
        ref int basePtr,
        ref StackFrame* callStackPtr
    ) // TODO: Make sure that FISR works even with doubles
    {
        byte a = (byte)((instruction >> 6) & 0xFF);
        uint b = (uint)((instruction >> 14) & 0xFF);
        double valB = b < 256 ? Reg(regPtr, basePtr, b) : constPtr[b - 256];
        long i;
        double x2,
            y;
        const float threehalfs = 1.5F;

        x2 = valB * 0.5d;
        y = valB;
        i = *(long*)&y; // evil floating point bit level hacking
        i = 0x5f3759df - (i >> 1); // what the fuck?
        y = *(double*)&i;
        y = y * (threehalfs - (x2 * y * y)); // 1st iteration
        //	y  = y * ( threehalfs - ( x2 * y * y ) );   // 2nd iteration, this can be removed
        Reg(regPtr, basePtr, a) = y;
        return true;
    }
}
