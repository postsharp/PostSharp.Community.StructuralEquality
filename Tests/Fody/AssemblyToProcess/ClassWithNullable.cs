using System;

namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
{
    [StructuralEquality]
    public class ClassWithNullable
    {
        public DateTime? NullableDate { get; set; }

        public static bool operator ==(ClassWithNullable left, ClassWithNullable right) => Operator.Weave(left, right);
        public static bool operator !=(ClassWithNullable left, ClassWithNullable right) => Operator.Weave(left, right);
    }
}