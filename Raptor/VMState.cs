using System.Text;

namespace Raptor;

public unsafe struct VMState
{
    public double* RegPtr;
    public double* ConstPtr;
    public uint* MethodTablePtr;
    public uint* InstPtr;
    public uint* Ip;
    public byte* HeapPtr;
    public int BasePtr;
    public uint FreeBlockHeaderPointer;
    public StackFrame* CallStackPtr;
    public uint RngState;
    public StringBuilder StringBuilder;
}
