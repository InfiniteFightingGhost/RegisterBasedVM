using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raptor.Attributes
{
/// <summary>
/// Marks a method for automatic FFI registration with the Raptor VM.
/// When no parameters are provided, the method name is converted to camelCase
/// and the index is auto-assigned.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RaptorMethodAttribute : Attribute
{
    /// <summary>
    /// The name used to reference this method in Raptor assembly scripts.
    /// If null, the C# method name is converted to camelCase.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// The method table index. -1 means auto-assign the next available index.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Registers with auto-generated camelCase name and auto-assigned index.
    /// </summary>
    public RaptorMethodAttribute()
    {
        Name = null;
        Index = -1;
    }

    /// <summary>
    /// Registers with an explicit name and auto-assigned index.
    /// </summary>
    public RaptorMethodAttribute(string name)
    {
        Name = name;
        Index = -1;
    }

    /// <summary>
    /// Registers with auto-generated camelCase name and an explicit index.
    /// </summary>
    public RaptorMethodAttribute(ushort index)
    {
        Name = null;
        Index = index;
    }

    /// <summary>
    /// Registers with an explicit name and explicit index.
    /// </summary>
    public RaptorMethodAttribute(string name, ushort index)
    {
        Name = name;
        Index = index;
    }
}
}
