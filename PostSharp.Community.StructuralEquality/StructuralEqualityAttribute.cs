using PostSharp.Extensibility;

namespace PostSharp.Community.StructuralEquality
{
    /// <summary>
    /// TODO
    /// </summary>
    [MulticastAttributeUsage(MulticastTargets.Class | MulticastTargets.Struct)]
    [RequirePostSharp("PostSharp.Community.StructuralEquality.Weaver", "StructuralEqualityTask")]
    public class StructuralEqualityAttribute : MulticastAttribute
    { 
    }
}