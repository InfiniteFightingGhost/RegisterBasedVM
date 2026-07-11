using System.Runtime.InteropServices;

namespace Raptor;

[StructLayout(LayoutKind.Explicit, Size = 88)]
public unsafe struct VMState
{
    [FieldOffset(0)]
    public double* RegPtr;

    [FieldOffset(8)]
    public double* ConstPtr;

    [FieldOffset(16)]
    public uint* MethodTablePtr;

    [FieldOffset(24)]
    public uint* InstPtr;

    [FieldOffset(32)]
    public uint* Ip; // Perfectly aligned to 16 bytes (32 % 16 == 0)

    [FieldOffset(40)]
    public byte* HeapPtr;

    [FieldOffset(48)]
    public int BasePtr;

    [FieldOffset(52)]
    public uint FreeBlockHeaderPointer;

    [FieldOffset(56)]
    public StackFrame* CallStackPtr;

    [FieldOffset(64)]
    public uint RngState;

    [FieldOffset(72)]
    public char* OutBufferPtr;

    [FieldOffset(80)]
    public int OutBufferCapacity;

    [FieldOffset(84)]
    public int OutBufferOffset;
}
