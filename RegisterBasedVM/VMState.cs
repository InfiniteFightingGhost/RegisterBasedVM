namespace RegisterBasedVM;

public unsafe struct VMState
{
    public double* RegPtr;
    public double* ConstPtr;
    public uint* MethodTablePtr;
    public uint* InstPtr;
    public int Pc;
    public int BasePtr;
    public StackFrame* CallStackPtr;
    public uint RngState;
}
