using System;
using System.Diagnostics.CodeAnalysis;

namespace PostSharp.Community.StructuralEquality
{
    /// <summary>
    /// Allows you to have PostSharp auto-generate <c>==</c> and <c>!=</c> operators for you.
    /// </summary>
    public static class Operator
    {
        /// <summary>
        /// Add the following code to a type annotated with [StructuralEquality] to auto-generate equality operators.
        /// <code>
        /// public static bool operator ==(YourClass left, YourClass right) => Operator.Weave(left, right);
        /// public static bool operator !=(YourClass left, YourClass right) => Operator.Weave(left, right);
        /// </code>
        /// Calls to this method are replaced at build time with appropriate code by PostSharp.
        /// </summary>
        public static bool Weave<T>(T left, T right) => throw WeavingNotWorkingException();
        static Exception WeavingNotWorkingException() => new Exception("PostSharp.Community.StructuralEquality was supposed to replace this method call with an implementation. Either weaving has not worked or you have called this method from an unsupported place. The only supported places are implementations of the `==` and `!=` operators in a type annotated with [StructuralEquality].");
    }
}