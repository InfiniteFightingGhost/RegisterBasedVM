namespace Raptor;

public class VMChunk
{
    private uint currUsedConstantsIndex = 0;
    public UInt32[] Instructions { get; set; }
    public double[] Constants { get; private set; } = new double[512];
    public uint[] MethodTable { get; private set; } = new uint[512];

    public uint SetConstant(double value)
    {
        for (int i = 0; i < currUsedConstantsIndex; i++)
        {
            if (Constants[i] == value)
            {
                return (uint)i;
            }
        }
        Constants[currUsedConstantsIndex] = value;
        return currUsedConstantsIndex++;
    }
}
