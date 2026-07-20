using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raptor.Attributes
{
    /// <summary>
    /// Provides a human-readable description for a Raptor FFI host method.
    /// Functions like &lt;summary&gt; but for Raptor script documentation and tooling.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RaptorDescriptionAttribute : Attribute
    {
        /// <summary>
        /// The description text for this method.
        /// </summary>
        public string Description { get; }

        public RaptorDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
