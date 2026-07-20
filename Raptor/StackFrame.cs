using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raptor
{
    /// <summary>
    /// Used to represent a call state in the call stack
    /// </summary>
    public readonly unsafe struct StackFrame
    {
        public readonly int ReturnPC;
        public readonly double* PreviousRegPtr;

        /// <summary>
        /// The constructor for the readonly frame we wish to create
        /// </summary>
        /// <param name="returnPC">The program counter that the VM will return to after the method returns</param>
        /// <param name="previousRegPtr">The register pointer that the VM will return to after the method returns</param>
        /// <seealso cref="VirtualMachine"/>
        public StackFrame(int returnPC, double* previousRegPtr)
        {
            ReturnPC = returnPC;
            PreviousRegPtr = previousRegPtr;
        }
    }
}
