﻿ namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
 {
     [StructuralEquality(TypeCheck = TypeCheck.SameTypeOrSubtype)]
     public class EqualsOrSubtypeClass
     {
         public int A { get; set; }

         public static bool operator ==(EqualsOrSubtypeClass left, EqualsOrSubtypeClass right) => Operator.Weave(left, right);
         public static bool operator !=(EqualsOrSubtypeClass left, EqualsOrSubtypeClass right) => Operator.Weave(left, right);
     }
 }