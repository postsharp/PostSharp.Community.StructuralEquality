﻿ namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
 {
     [StructuralEquality]
     public class ClassWithGenericProperty
     {
         public GenericDependency<int> Prop { get; set; }

         public static bool operator ==(ClassWithGenericProperty left, ClassWithGenericProperty right) => Operator.Weave(left, right);
         public static bool operator !=(ClassWithGenericProperty left, ClassWithGenericProperty right) => Operator.Weave(left, right);
     }
 }