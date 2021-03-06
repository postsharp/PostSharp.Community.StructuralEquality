using System;

namespace PostSharp.Community.StructuralEquality
{
    /// <summary>
    /// Custom method marker. The annotated method must have the signature <c>bool MethodName(Type)</c>, where <c>Type</c>
    /// is the type that contains the method. The method is called by the auto-generated equality comparison after
    /// all generated code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AdditionalEqualsMethodAttribute : Attribute
    {
    }
}