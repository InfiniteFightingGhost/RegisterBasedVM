using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace Raptor
{
///<summary>
///The struct that unifies all bit fiddling logic into a simple and bug-free solution.
///</summary>
///<remarks>
///Every bytecode packing and unpacking needs to go through here.
///No manual bit fiddling is allowed for instructions.
///</remarks>
public readonly struct Instruction
{
    public readonly uint Value;

    private const int OpCodeBits = 6;
    private const int ABits = 8;
    private const int BBits = 9;
    private const int CBits = 9;
    private const int BxBits = 18;
    private const int sBx16Bits = 16;
    private const int sBx26Bits = 26;

    private const int AShift = OpCodeBits;
    private const int BShift = AShift + ABits;
    private const int CShift = BShift + BBits;

    private const uint OpCodeMask = (1 << OpCodeBits) - 1;
    private const uint AMask = (1 << ABits) - 1;
    private const uint BMask = (1 << BBits) - 1;
    private const uint CMask = (1 << CBits) - 1;
    private const uint BxMask = (1 << BxBits) - 1;
    private const uint sBx16Mask = (1 << sBx16Bits) - 1;
    private const uint sBx26Mask = (1 << sBx26Bits) - 1;

    public Instruction(uint value) => Value = value;

    public OpCode Op => (OpCode)(Value & OpCodeMask);
    public byte A => (byte)((Value >> AShift) & AMask);
    public ushort B => (ushort)((Value >> BShift) & BMask);
    public ushort C => (ushort)((Value >> CShift) & CMask);
    public uint Bx => (Value >> BShift) & BxMask;
    private const int sBx16Bias = 32767;
    public int sBx16 => (int)((Value >> BShift) & sBx16Mask) - sBx16Bias;

    private const int sBx26Bias = 33554431;
    public int sBx26 => (int)((Value >> AShift) & sBx26Mask) - sBx26Bias;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Instruction CreateABC(OpCode op, byte a, ushort b, ushort c)
    {
        uint val = (uint)op | ((uint)a << AShift) | ((uint)b << BShift) | ((uint)c << CShift);
        return new Instruction(val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Instruction CreateABx(OpCode op, byte a, uint bx)
    {
        uint val = (uint)op | ((uint)a << AShift) | (bx << BShift);
        return new Instruction(val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Instruction CreateAsBx(OpCode op, byte a, int sbx)
    {
        uint biasedBx = (uint)(sbx + sBx16Bias) & sBx16Mask;
        uint val = (uint)op | ((uint)a << AShift) | (biasedBx << BShift);
        return new Instruction(val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Instruction CreateSBx26(OpCode op, int sBxOffset)
    {
        uint biasedBx = (uint)(sBxOffset + sBx26Bias) & 0x3FFFFFF;

        uint val = (uint)op | (biasedBx << AShift);

        return new Instruction(val);
    }

    public static implicit operator uint(Instruction i) => i.Value;
}
}
