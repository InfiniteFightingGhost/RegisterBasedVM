namespace Raptor;

public readonly struct StackFrame
{
    public readonly int ReturnPC;
    public readonly int PreviousBase;

    public StackFrame(int returnPC, int previousBase)
    {
        ReturnPC = returnPC;
        PreviousBase = previousBase;
    }
}
