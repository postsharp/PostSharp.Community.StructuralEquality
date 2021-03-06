﻿using System.Collections.Generic;

namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.GenericClass
{
    [StructuralEquality]
    public class GenericClass<T>
        where T : GenericClassBaseClass
    {
        public int a;

        public int A
        {
            get => a;
            set => a = value;
        }

        public IEnumerable<T> B { get; set; }

        public static bool operator ==(GenericClass<T> left, GenericClass<T> right) => Operator.Weave(left, right);
        public static bool operator !=(GenericClass<T> left, GenericClass<T> right) => Operator.Weave(left, right);
    }
}