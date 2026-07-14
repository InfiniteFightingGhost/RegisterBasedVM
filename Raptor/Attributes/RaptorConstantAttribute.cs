/// <summary>
/// Marks a constant for automatic constant registration.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class RaptorConstantAttribute : Attribute
{
    /// <summary>
    /// The name used to reference this constant in Raptor assembly scripts.
    /// If null, the C# constant name is converted to camelCase.
    /// </summary>
    public string? Name { get; }
}
