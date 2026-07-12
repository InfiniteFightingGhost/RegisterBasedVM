using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raptor
{
///<summary>
///Represents a chunk of instructions alongside the constants and method id's they use
///</summary>
public class VMChunk
{
    private uint currUsedConstantsIndex = 0;
    public UInt32[] Instructions { get; set; } = System.Array.Empty<uint>();
    public double[] Constants { get; internal set; } = new double[512];
    public uint[] MethodTable { get; internal set; } = new uint[512];
    public Compiler.SourceMap? SourceMap { get; set; }

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
}
