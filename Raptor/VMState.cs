using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Raptor
{
    ///<summary>
    ///This struct represents everything that the <see cref="VirtualMachine"/> will need in order to do it's magic.
    ///</summary>
    ///<remarks>
    ///A little on the big side, don't you think?
    ///</remarks>
    [StructLayout(LayoutKind.Sequential)]
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
        public bool HasError;
    }
}
