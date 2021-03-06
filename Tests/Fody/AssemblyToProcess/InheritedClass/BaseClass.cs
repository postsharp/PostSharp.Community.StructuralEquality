﻿namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.InheritedClass
{
    [StructuralEquality]
    public class BaseClass
    {
        public int A { get; set; }

        public static bool operator ==(BaseClass left, BaseClass right) => Operator.Weave(left, right);
        public static bool operator !=(BaseClass left, BaseClass right) => Operator.Weave(left, right);
    }
}