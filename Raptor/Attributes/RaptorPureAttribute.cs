using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raptor.Attributes
{
    /// <summary>
    /// Marks a Raptor FFI host method as pure (side-effect-free).
    /// Pure methods always return the same output for the same inputs and do not
    /// modify any external state. This metadata is stored in the FFI host table
    /// and can be used by the VM for caching and optimization in the future.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RaptorPureAttribute : Attribute { }
}
