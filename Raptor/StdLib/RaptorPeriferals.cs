using Raptor;
using Raptor.Attributes;

[RaptorModule("peri")]
public static class RaptorPeriferals
{
    [RaptorMethod]
    [RaptorDescription("Prints text to the console")]
    public static void Print(ref VMState state)
    {
        unsafe
        {
            Console.WriteLine(state.RegPtr[0]);
        }
    }
}
