﻿namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
{
    [StructuralEquality]
    public class NormalClass
    {
        public int X { get; set; }

        public string Y { get; set; }

        public double Z { get; set; }

        public char V { get; set; }

        public static bool operator ==(NormalClass left, NormalClass right) => Operator.Weave(left, right);
        public static bool operator !=(NormalClass left, NormalClass right) => Operator.Weave(left, right);
    }
}