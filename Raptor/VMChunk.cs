namespace Raptor;

///<summary>
///Represents a chunk of instructions alongside the constants and method id's they use
///</summary>
public class VMChunk
{
    private uint currUsedConstantsIndex = 0;
    public UInt32[] Instructions { get; set; }
    public double[] Constants { get; internal set; } = new double[512];
    public uint[] MethodTable { get; internal set; } = new uint[512];

    ///<summary>
    ///Used to add a constant to the Constants array in order for the instructions to access it
    ///</summary>
    ///<param name="value">The value that will be saved in the array</param>
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
