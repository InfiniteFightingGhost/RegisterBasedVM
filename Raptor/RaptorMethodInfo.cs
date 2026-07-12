using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raptor
{
/// <summary>
/// Holds metadata about a registered Raptor FFI host method.
/// Populated during reflection-based registration and available for
/// tooling, documentation generation, and VM optimization.
/// </summary>
public sealed class RaptorMethodInfo
{
    /// <summary>
    /// The name used to reference this method in Raptor assembly scripts.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The method table index this method is registered at.
    /// </summary>
    public ushort Index { get; }

    /// <summary>
    /// Optional human-readable description of the method's purpose.
    /// Populated from <see cref="Attributes.RaptorDescriptionAttribute"/>.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Whether this method is marked as pure (side-effect-free).
    /// Populated from <see cref="Attributes.RaptorPureAttribute"/>.
    /// </summary>
    public bool IsPure { get; }

    /// <summary>
    /// Optional parameter descriptions, keyed by parameter name.
    /// Populated from <see cref="Attributes.RaptorParamAttribute"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ParameterDescriptions { get; }

    /// <summary>
    /// The names of all register-mapped parameters exposed to the scripting engine.
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<string> ParameterNames { get; }

    public RaptorMethodInfo(
        string name,
        ushort index,
        string? description,
        bool isPure,
        IReadOnlyDictionary<string, string>? parameterDescriptions,
        System.Collections.Generic.IReadOnlyList<string> parameterNames
    )
    {
        Name = name;
        Index = index;
        Description = description;
        IsPure = isPure;
        ParameterDescriptions = parameterDescriptions;
        ParameterNames = parameterNames;
    }
}
}
