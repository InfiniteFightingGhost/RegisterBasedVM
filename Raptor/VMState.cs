using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Raptor
{
[StructLayout(LayoutKind.Sequential)]
///<summary>
///This struct represents everything that the <see cref="VirtualMachine"/> will need in order to do it's magic.
///</summary>
///<remarks
///A little on the big side, don't you think?
///</remarks>
public unsafe struct VMState
{
    public double* RegPtr;
    public double* ConstPtr;
    public uint* MethodTablePtr;

    ///<summary>Points to the absolute address of the instruction array.</summary>
    public uint* InstPtr;

    ///<summary>Points to the current instruction.</summary>
    public uint* Ip;
    public byte* HeapPtr;

    /// <summary>
    /// The value that the registers are incremented by so that the method calls can have sandboxing +
    /// zero instructions for moving around variables for parameters.
    /// </summary>
    public int BasePtr;

    ///<summary>
    ///Points to the head of the instrisically linked list of free blocks in the VM's heap.
    ///</summary>
    public uint FreeBlockHeaderPointer;
    public StackFrame* CallStackPtr;
    public StackFrame* CallStackLimit;

    ///<summary>
    ///Current seed state used for the VM's internal pseudo-random number generator.
    ///</summary>
    public uint RngState;
    public char* OutBufferPtr;
    public int OutBufferCapacity;
    public int OutBufferOffset;
}
}
