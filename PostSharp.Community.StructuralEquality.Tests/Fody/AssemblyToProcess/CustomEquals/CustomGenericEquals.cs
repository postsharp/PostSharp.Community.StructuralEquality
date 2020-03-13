﻿namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.CustomEquals
{
    [StructuralEquality]
    public struct CustomGenericEquals<T>
    {
        [IgnoreDuringEquals]
        public T Prop { get; set; }

        [AdditionalEqualsMethod]
        bool CustomEquals(CustomGenericEquals<T> other)
        {
            return Equals(Prop, other.Prop);
        }

        [AdditionalGetHashCodeMethod]
        int CustomGetHashCode()
        {
            return 42;
        }

        public static bool operator ==(CustomGenericEquals<T> left, CustomGenericEquals<T> right) => Operator.Weave(left, right);
        public static bool operator !=(CustomGenericEquals<T> left, CustomGenericEquals<T> right) => Operator.Weave(left, right);
    }
}