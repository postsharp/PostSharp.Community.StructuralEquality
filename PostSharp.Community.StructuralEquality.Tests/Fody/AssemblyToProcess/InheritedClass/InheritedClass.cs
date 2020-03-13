﻿namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.InheritedClass
{
    [StructuralEquality]
    public class InheritedClass :
        BaseClass
    {
        public int B { get; set; }

        public static bool operator ==(InheritedClass left, InheritedClass right) => Operator.Weave(left, right);
        public static bool operator !=(InheritedClass left, InheritedClass right) => Operator.Weave(left, right);
    }
}