using System;

namespace PostSharp.Community.StructuralEquality
{
    /// <summary>
    /// Custom method marker. The annotated method must have the signature <c>int MethodName()</c>. The method
    /// is called by the auto-generated GetHashCode method after all generated code, and it's combined with the generated
    /// hash code with a Fowler–Noll–Vo algorithm. There can be only one additional method like this.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AdditionalGetHashCodeMethodAttribute : Attribute
    {
    }
}