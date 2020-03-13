using PostSharp.Extensibility;

namespace PostSharp.Community.StructuralEquality
{
    /// <summary>
    /// The annotated field or property is ignored during the generation of Equals and GetHashCode methods.
    /// </summary>
    [MulticastAttributeUsage(MulticastTargets.Field | MulticastTargets.Property)]
    public sealed class IgnoreDuringEqualsAttribute : MulticastAttribute
    {
    }
}