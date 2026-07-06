using System.Runtime.CompilerServices;

namespace RegisterBasedVM;

public readonly struct Instruction
{
    public readonly uint Value;

    private const int OpCodeBits = 6;
    private const int ABits = 8;
    private const int BBits = 9;
    private const int CBits = 9;
    private const int BxBits = 18;
    private const int sBx26Bits = 26;

    private const int AShift = OpCodeBits;
    private const int BShift = AShift + ABits;
    private const int CShift = BShift + BBits;

    private const uint OpCodeMask = (1 << OpCodeBits) - 1;
    private const uint AMask = (1 << ABits) - 1;
    private const uint BMask = (1 << BBits) - 1;
    private const uint CMask = (1 << CBits) - 1;
    private const uint BxMask = (1 << BxBits) - 1;
    private const uint sBx26Mask = (1 << sBx26Bits) - 1;

    public Instruction(uint value) => Value = value;

    public OpCode Op => (OpCode)(Value & OpCodeMask);
    public byte A => (byte)((Value >> AShift) & AMask);
    public ushort B => (ushort)((Value >> BShift) & BMask);
    public ushort C => (ushort)((Value >> CShift) & CMask);
    public uint Bx => (Value >> BShift) & BxMask;

    public int sBx => (int)Bx - ((1 << (BxBits - 1)) - 1);

    public int sBx26 => (int)((Value >> AShift) & sBx26Mask) - 33554431;

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
    public static Instruction CreateSBx26(OpCode op, int sBxOffset)
    {
        const int sBxBias = 33554431;
        uint biasedBx = (uint)(sBxOffset + sBxBias) & 0x3FFFFFF;

        uint val = (uint)op | (biasedBx << 6);

        return new Instruction(val);
    }

    public static implicit operator uint(Instruction i) => i.Value;
}
