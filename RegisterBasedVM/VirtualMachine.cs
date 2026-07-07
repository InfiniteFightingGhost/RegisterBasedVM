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
    private const int _heapSize = 4 * 1024 * 1024;
    private byte[] _heap = new byte[_heapSize];
    uint rngState = 2463534215; // RNG seed

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
                InstPtr = instPtr,
                Pc = 0,
                BasePtr = 0,
                CallStackPtr = framePtr,
                RngState = rngState,
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
                Thread.Sleep(50);
                Console.WriteLine(
                    $"[TRACE] PC:{state.Pc:D4} | Op:{instruction.Op, -8} | A:{instruction.A, -3} | B:{instruction.B, -3} | C:{instruction.C, -3} | Bx:{instruction.Bx, -5} | sBx:{instruction.sBx26}"
                );
#endif
                // isRunning = dispatchTable[(int)instruction.Op](instruction, ref state);
                switch (instruction.Op)
                {
                    case OpCode.LOADC:
                        isRunning = ExecuteLoadC(instruction, ref state);
                        break;
                    case OpCode.MOVE:
                        isRunning = ExecuteMove(instruction, ref state);
                        break;
                    case OpCode.UNM:
                        isRunning = ExecuteUnm(instruction, ref state);
                        break;
                    case OpCode.SWAP:
                        isRunning = ExecuteSwap(instruction, ref state);
                        break;
                    case OpCode.ADD:
                        isRunning = ExecuteAdd(instruction, ref state);
                        break;
                    case OpCode.SUB:
                        isRunning = ExecuteSub(instruction, ref state);
                        break;
                    case OpCode.MUL:
                        isRunning = ExecuteMul(instruction, ref state);
                        break;
                    case OpCode.DIV:
                        isRunning = ExecuteDiv(instruction, ref state);
                        break;
                    case OpCode.POW:
                        isRunning = ExecutePow(instruction, ref state);
                        break;
                    case OpCode.SQRT:
                        isRunning = ExecuteSqrt(instruction, ref state);
                        break;
                    case OpCode.FISR:
                        isRunning = ExecuteFisr(instruction, ref state);
                        break;
                    case OpCode.JUMP:
                        isRunning = ExecuteJump(instruction, ref state);
                        break;
                    case OpCode.CALL:
                        isRunning = ExecuteCall(instruction, ref state);
                        break;
                    case OpCode.RETURN:
                        isRunning = ExecuteReturn(instruction, ref state);
                        break;
                    case OpCode.PRINT:
                        isRunning = ExecutePrint(instruction, ref state);
                        break;
                    case OpCode.PRINTA:
                        isRunning = ExecutePrintA(instruction, ref state);
                        break;
                    case OpCode.EQ:
                        isRunning = ExecuteEq(instruction, ref state);
                        break;
                    case OpCode.LT:
                        isRunning = ExecuteLt(instruction, ref state);
                        break;
                    case OpCode.LE:
                        isRunning = ExecuteLe(instruction, ref state);
                        break;
                    case OpCode.HALT:
                        isRunning = ExecuteHalt(instruction, ref state);
                        break;
                    case OpCode.RAND:
                        isRunning = ExecuteRand(instruction, ref state);
                        break;
                    case OpCode.FOR:
                        isRunning = ExecuteFor(instruction, ref state);
                        break;
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
    public static unsafe bool ExecuteSwap(Instruction instruction, ref VMState state)
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
    public static unsafe bool ExecuteMod(Instruction instruction, ref VMState state) // TODO: Make sure that FISR works even with doubles
    {
        byte a = instruction.A;
        ushort b = instruction.B;
        double valB = b < 256 ? Reg(state.RegPtr, state.BasePtr, b) : state.ConstPtr[b - 256];
        ushort c = instruction.C;
        double valC = c < 256 ? Reg(state.RegPtr, state.BasePtr, c) : state.ConstPtr[c - 256];
        Reg(state.RegPtr, state.BasePtr, a) = valB % valC;
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
        state.RngState ^= state.RngState << 13;
        state.RngState ^= state.RngState >> 17;
        state.RngState ^= state.RngState << 5;
        double result = state.RngState * 2.3283064365386963e-10;
        Reg(state.RegPtr, state.BasePtr, a) = result;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool ExecuteFor(Instruction instruction, ref VMState state)
    {
        byte index = instruction.A;
        ushort max = instruction.B;
        ushort step = instruction.C;

        double valIndex = Reg(state.RegPtr, state.BasePtr, index);
        double valMax =
            max < 256 ? Reg(state.RegPtr, state.BasePtr, max) : state.ConstPtr[max - 256];
        double valStep =
            step < 256 ? Reg(state.RegPtr, state.BasePtr, step) : state.ConstPtr[step - 256];
        valIndex += valStep;
        Reg(state.RegPtr, state.BasePtr, index) = valIndex;
        Instruction secondInst = new Instruction(state.InstPtr[++state.Pc]);
        byte condition = secondInst.A;
        bool conditionMet = false;
        switch (condition)
        {
            case 0:
                conditionMet = (valIndex < valMax);
                break;
            case 1:
                conditionMet = (valIndex > valMax);
                break;
            case 2:
                conditionMet = (valIndex <= valMax);
                break;
            case 3:
                conditionMet = (valIndex >= valMax);
                break;
        }
        if (conditionMet)
        {
            int jumpOffset = secondInst.sBx16;
            state.Pc += jumpOffset - 2;
        }
        return true;
    }
}
