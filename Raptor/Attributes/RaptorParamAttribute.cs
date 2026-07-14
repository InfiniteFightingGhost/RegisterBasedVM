using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raptor.Attributes
{
    /// <summary>
    /// Provides a human-readable description for a parameter on a Raptor FFI host method.
    /// Applied directly to the parameter declaration.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Parameter | AttributeTargets.Method,
        AllowMultiple = true,
        Inherited = false
    )]
    public sealed class RaptorParamAttribute : Attribute
    {
        /// <summary>
        /// Overrides the name of the method's parameter.
        /// </summary>
        /// <remarks>
        /// If using <see cref="ScriptEngine.RegisterHostMethod(string, ushort, VirtualMachine.HostFFIDelegate)"/> or
        /// using the attrtibute approach of registering methods using "ref VMState" make sure to provide names for the params
        /// or else you will get zero parameter descriptions.
        /// </remarks
        public string? ParamName { get; set; }

        /// <summary>
        /// The description text for this parameter.
        /// </summary>
        public string Description { get; }

        public RaptorParamAttribute(string description)
        {
            Description = description;
        }

        public RaptorParamAttribute(string paramName, string description)
        {
            ParamName = paramName;
            Description = description;
        }
    }
}
