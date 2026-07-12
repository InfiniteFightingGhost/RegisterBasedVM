using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raptor
{
/// <summary>
/// Used to represent a call state in the call stack
/// </summary>
public readonly struct StackFrame
{
    public readonly int ReturnPC;
    public readonly int PreviousBase;

    /// <summary>
    /// The constructor for the readonly frame we wish to create
    /// </summary>
    /// <param name="returnPC">The program counter that the VM will return to after the method returns</param>
    /// <param name="previousBase">The base that the VM will return after the method returns</param>
    /// <seealso cref="VirtualMachine"/>
    public StackFrame(int returnPC, int previousBase)
    {
        ReturnPC = returnPC;
        PreviousBase = previousBase;
    }
}
}
