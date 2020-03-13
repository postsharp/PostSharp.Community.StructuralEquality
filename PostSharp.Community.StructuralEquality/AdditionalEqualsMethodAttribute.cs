using System;

namespace PostSharp.Community.StructuralEquality
{
    /// <summary>
    /// Custom method marker. The annotated method must have the signature <c>bool MethodName(object)</c>. The method
    /// is called by the auto-generated equality comparison after all generated code. There can be only one additional
    /// method like this.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AdditionalEqualsMethodAttribute : Attribute
    {
    }
}