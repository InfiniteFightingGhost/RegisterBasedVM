#if !NET5_0_OR_GREATER

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata on record types.
    /// Necessary for C# 9.0 record compilation on older Unity .NET runtimes.
    /// </summary>
    internal static class IsExternalInit {}
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresUnreferencedCodeAttribute : Attribute
    {
        public string Message { get; }
        public string? Url { get; set; }
        public RequiresUnreferencedCodeAttribute(string message) => Message = message;
    }

    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property | 
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Parameter | AttributeTargets.ReturnValue | 
        AttributeTargets.GenericParameter, 
        Inherited = false)]
    internal sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        public DynamicallyAccessedMemberTypes Types { get; }
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes types) => Types = types;
    }

    [Flags]
    internal enum DynamicallyAccessedMemberTypes
    {
        None = 0,
        PublicParameterlessConstructor = 1,
        PublicConstructors = 2,
        NonPublicConstructors = 4,
        PublicMethods = 8,
        NonPublicMethods = 16,
        PublicFields = 32,
        NonPublicFields = 64,
        PublicNestedTypes = 128,
        NonPublicNestedTypes = 256,
        PublicProperties = 512,
        NonPublicProperties = 1024,
        PublicEvents = 2048,
        NonPublicEvents = 4096,
        Interfaces = 8192,
        All = -1
    }
}

#endif
