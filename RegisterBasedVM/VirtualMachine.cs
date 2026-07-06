using System.Runtime.CompilerServices;

namespace RegisterBasedVM;

public unsafe class VirtualMachine
{
    private uint[] _instructions = null!;
    private double[] _constants = null!;
    int Pc = 0;
    int BasePtr = 0;
    private static readonly Random random = new Random();
    private int[] _breakpoints = null!;
    private uint[] _methods = null!;

    public void LoadProgram(VMChunk chunk, int[] breakpoints)
    {
        _instructions = chunk.Instructions;
        _constants = chunk.Constants;
        _methods = chunk.MethodTable;
        Pc = 0;
        BasePtr = 0;
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
        delegate* <Instruction, ref VMState, bool>* dispatchTable =
            stackalloc delegate* <Instruction, ref VMState, bool>[64];

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

        double* RegPtr = stackalloc double[256];
        Unsafe.InitBlockUnaligned(RegPtr, 0, 256 * sizeof(double));

        StackFrame* framePtr = stackalloc StackFrame[32];
        fixed (uint* instPtr = _instructions)
        fixed (double* constPtr = _constants)
        fixed (uint* methodTablePtr = _methods)
        {
            bool isRunning = true;
            Console.WriteLine("Starting VM...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            VMState state = new VMState
            {
                RegPtr = RegPtr,
                ConstPtr = constPtr,
                MethodTablePtr = methodTablePtr,
                Pc = 0,
                BasePtr = 0,
                CallStackPtr = framePtr,
            };
            while (isRunning)
            {
                Instruction instruction = new Instruction(instPtr[state.Pc]);
#if DEBUG
                if (_breakpoints.Contains(Pc))
                {
                    DumpRegisters(state.RegPtr);
                    Console.ReadLine();
                }
                Console.WriteLine(
                    $"[TRACE] PC:{state.Pc:D4} | Op:{instruction.Op, -8} | A:{instruction.A, -3} | B:{instruction.B, -3} | C:{instruction.C, -3} | Bx:{instruction.Bx, -5} | sBx:{instruction.sBx26}"
                );
#endif
                try
                {
                    isRunning = dispatchTable[(int)instruction.Op](instruction, ref state);
                }
                catch (NullReferenceException ex)
                {
                    Console.WriteLine(ex);
                    Console.WriteLine("Opcode was " + (int)instruction.Op);
                    return;
                }
                state.Pc++;
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
    private static unsafe ref double Reg(double* RegPtr, int BasePtr, uint index)
    {
        return ref RegPtr[BasePtr + index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteLoadC(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        uint constantIndex = instruction.Bx;
        Reg(state.RegPtr, state.BasePtr, a) = state.ConstPtr[constantIndex];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteMove(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        byte b = (byte)instruction.B;
        Reg(state.RegPtr, state.BasePtr, a) = Reg(state.RegPtr, state.BasePtr, b);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteSwp(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        byte b = (byte)instruction.B;
        (Reg(state.RegPtr, state.BasePtr, a), Reg(state.RegPtr, state.BasePtr, b)) = (
            Reg(state.RegPtr, state.BasePtr, b),
            Reg(state.RegPtr, state.BasePtr, a)
        );
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteUnm(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
        Reg(state.RegPtr, state.BasePtr, a) = -valB;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteJump(Instruction instruction, ref VMState state)
    {
        state.Pc += instruction.sBx26 - 1;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteCall(Instruction instruction, ref VMState state)
    {
        byte start = instruction.A;
        ushort methodIndex = instruction.B;
        StackFrame frame = new StackFrame(state.Pc, state.BasePtr);
        CallStackPush(ref state.CallStackPtr, frame);
        state.BasePtr += start;

        state.Pc = (int)state.MethodTablePtr[methodIndex];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteReturn(Instruction instruction, ref VMState state)
    {
        byte start = instruction.A;
        byte end = (byte)instruction.B;
        byte count = (byte)(end - start);
        for (uint i = 0; i <= count; i++)
        {
            Reg(state.RegPtr, state.BasePtr, i) = Reg(state.RegPtr, state.BasePtr, start + i);
        }
        StackFrame frame = CallStackPop(ref state.CallStackPtr);
        int target = frame.ReturnPC;
        state.BasePtr = frame.PreviousBase;
        state.Pc = target;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteAdd(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
        ushort c = instruction.C;
        double valC = c < 256 ? Reg(state.RegPtr, state.BasePtr, c) : state.ConstPtr[c - 256];
        Reg(state.RegPtr, state.BasePtr, a) = valB + valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteSub(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
        ushort c = instruction.C;
        double valC = c < 256 ? Reg(state.RegPtr, state.BasePtr, c) : state.ConstPtr[c - 256];
        Reg(state.RegPtr, state.BasePtr, a) = valB - valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteMul(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
        ushort c = instruction.C;
        double valC = c < 256 ? Reg(state.RegPtr, state.BasePtr, c) : state.ConstPtr[c - 256];
        Reg(state.RegPtr, state.BasePtr, a) = valB * valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteDiv(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
        ushort c = instruction.C;
        double valC = c < 256 ? Reg(state.RegPtr, state.BasePtr, c) : state.ConstPtr[c - 256];
        Reg(state.RegPtr, state.BasePtr, a) = valB / valC;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecutePow(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
        ushort c = instruction.C;
        double valC = c < 256 ? Reg(state.RegPtr, state.BasePtr, c) : state.ConstPtr[c - 256];
        Reg(state.RegPtr, state.BasePtr, a) = (float)Math.Pow(valB, valC);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteEq(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
        ushort c = instruction.C;
        double valC = c < 256 ? Reg(state.RegPtr, state.BasePtr, c) : state.ConstPtr[c - 256];
        bool comparison = valB == valC;
        bool expected = (a != 0);
        if (comparison == expected)
        {
            state.Pc++;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteLt(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
        ushort c = instruction.C;
        double valC = c < 256 ? Reg(state.RegPtr, state.BasePtr, c) : state.ConstPtr[c - 256];
        bool comparison = valB < valC;

        bool expected = (a != 0);
        if (comparison != expected)
        {
            state.Pc++;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteLe(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
        ushort c = instruction.C;
        double valC = c < 256 ? Reg(state.RegPtr, state.BasePtr, c) : state.ConstPtr[c - 256];
        bool comparison = valB <= valC;
        bool expected = (a != 0);
        if (comparison == expected)
        {
            state.Pc++;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecutePrint(Instruction instruction, ref VMState state)
    {
        uint a = (uint)instruction.B;
        double valA = a < 256 ? Reg(state.RegPtr, state.BasePtr, a) : state.ConstPtr[a - 256];
        Console.WriteLine(valA);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecutePrintA(Instruction instruction, ref VMState state)
    {
        uint a = (uint)instruction.B;
        double valA = a < 256 ? Reg(state.RegPtr, state.BasePtr, a) : state.ConstPtr[a - 256];
        Console.Write((char)valA);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteHalt(Instruction instruction, ref VMState state)
    {
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteRand(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        Reg(state.RegPtr, state.BasePtr, a) = random.NextSingle();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteSqrt(Instruction instruction, ref VMState state)
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
        Reg(state.RegPtr, state.BasePtr, a) = (float)Math.Sqrt(valB);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteFisr(Instruction instruction, ref VMState state) // TODO: Make sure that FISR works even with doubles
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
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
        Reg(state.RegPtr, state.BasePtr, a) = y;
        return true;
    }
}
