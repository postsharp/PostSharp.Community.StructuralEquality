
namespace PostSharp.Community.StructuralEquality
{
    /// <summary>
    /// Defines requirements on the type of the comparand in the <see cref="object.Equals(object)"/> method.
    /// </summary>
    public enum TypeCheck
    {
        /// <summary>
        /// The 'this' and 'other' comparands must have the exact same type for equality to pass.
        /// </summary>
        ExactlyTheSameTypeAsThis,
        /// <summary>
        /// The 'other' comparand must be of the exact type that's annotated with this <see cref="StructuralEqualityAttribute"/>.  
        /// </summary>
        ExactlyOfType,
        /// <summary>
        /// The 'other' comparand must be of the type that's annotated with this <see cref="StructuralEqualityAttribute"/> or it must be a subtype of this type. 
        /// </summary>
        SameTypeOrSubtype
    }
}