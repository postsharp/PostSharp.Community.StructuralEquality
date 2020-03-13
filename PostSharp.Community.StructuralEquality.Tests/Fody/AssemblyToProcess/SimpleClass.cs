﻿using System;

namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
{
    [StructuralEquality]
    public class SimpleClass
    {
        public int Value { get; set; }

        public string Text { get; set; }

        public DateTime Date { get; set; }

        public static bool operator ==(SimpleClass left, SimpleClass right) => Operator.Weave(left, right);
        public static bool operator !=(SimpleClass left, SimpleClass right) => Operator.Weave(left, right);
    }
}