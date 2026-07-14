using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace Raptor
{
///<summary>
///A VM's status after it has exited it run method(by design or not).
///</summary>
public enum VMStatus
{
    Success,
    Halted,
    OutOfMemory,
    DivisionByZero,
    StackOverflow,
    InvalidInstruction,
    GasExceeded,
    HostError,
}

///<summary>
///A struct representing the result of both <see cref="VirtualMachine.RunFast()"/> and <see cref="VirtualMachine.RunDebug(VirtualMachine.DebugHook)"/.
///</summary>
public struct ExecutionResult
{
    public VMStatus Status;

    ///<summary>Represent the instruction pointer that the program exited on.</summary>
    public int IpOffset;
    public double[] RegistersSnapshot;
    public StackFrame[] CallStackSnapshot;
    public string? ErrorMessage;
    public ulong[]? OpcodeCounters;
    public ulong TotalInstructions;
}

///<summary>
///Used to handle VM exceptions.
///</summary>
///<remarks>
///When all goes wrong the big red button seems the way to go.
///</remarks>
public class VMPanicException : Exception
{
    public VMStatus Status { get; }
    public int IpOffset { get; }

    public VMPanicException(VMStatus status, int ipOffset, string message)
        : base(message)
    {
        Status = status;
        IpOffset = ipOffset;
    }
}
}
