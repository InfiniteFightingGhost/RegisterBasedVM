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
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class RaptorParamAttribute : Attribute
{
    /// <summary>
    /// The description text for this parameter.
    /// </summary>
    public string Description { get; }

    public RaptorParamAttribute(string description)
    {
        Description = description;
    }
}
}
