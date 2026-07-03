namespace RegisterBasedVM;

public class VMChunk
{
    private uint currUsedConstantsIndex = 0;
    public UInt32[] Instructions { get; set; }
    public float[] Constants { get; private set; } = new float[512];

    public uint SetConstant(float value)
    {
        var index = Constants.IndexOf(value);
        if (index != -1)
        {
            return (uint)index;
        }
        Constants[currUsedConstantsIndex] = value;
        return currUsedConstantsIndex++;
    }
}
