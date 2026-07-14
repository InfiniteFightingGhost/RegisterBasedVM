using Raptor;
using Raptor.Attributes;

[RaptorModule("peri")]
public static class RaptorPeriferals
{
    [RaptorMethod]
    [RaptorDescription("Prints text on the console")]
    [RaptorParam("number", "The number that will be printed to the console.")]
    public static void Print(ref VMState state)
    {
        unsafe
        {
            Console.WriteLine(state.RegPtr[0]);
        }
    }
}
