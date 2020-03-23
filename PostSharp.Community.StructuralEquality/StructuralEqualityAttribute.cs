using System;
using PostSharp.Extensibility;

namespace PostSharp.Community.StructuralEquality
{
    /// <summary>
    /// PostSharp creates an Equals and GetHashCode implementation for the target annotated type. See the GitHub
    /// repository for this add-in for options and details on how the implementation works.
    /// </summary>
    [MulticastAttributeUsage(MulticastTargets.Class | MulticastTargets.Struct)]
    [RequirePostSharp("PostSharp.Community.StructuralEquality.Weaver", "StructuralEqualityTask")]
    public class StructuralEqualityAttribute : MulticastAttribute
    {
        /// <summary>
        /// If true, PostSharp does not create the <see cref="object.GetHashCode"/> method. If you supply your own GetHashCode method
        /// in the annotated type, you don't need to set this property. Your code will take precedence.
        /// </summary>
        public bool DoNotAddGetHashCode { get; set; } = false;

        /// <summary>
        /// If true, PostSharp does not create the <see cref="object.Equals(object)"/> method. If you supply your own Equals method
        /// in the annotated type, you don't need to set this property. Your code will take precedence.
        /// </summary>
        public bool DoNotAddEquals { get; set; } = false;
        
        /// <summary>
        /// Specifies requirements on the type of the comparand in the <see cref="object.Equals(object)"/> method.
        /// </summary>
        public TypeCheck TypeCheck { get; set; }
        
        /// <summary>
        /// If the annotated type has any string fields or properties, this determines the way they're compared for
        /// equality.
        /// </summary>
        public StringComparison StringComparisonStyle { get; set; }

        /// <summary>
        /// If true, <c>base.Equals()</c> is not called in the generated <c>Equals</c> method. The same is true for
        /// <c>GetHashCode</c>.
        /// </summary>
        public bool IgnoreBaseClass { get; set; }

    }
}