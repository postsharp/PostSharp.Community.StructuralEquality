﻿ namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
 {
     [StructuralEquality(TypeCheck = TypeCheck.SameTypeOrSubtype)]
     public struct EqualsOrSubtypeStruct
     {
         public int A { get; set; }

         public static bool operator ==(EqualsOrSubtypeStruct left, EqualsOrSubtypeStruct right) => Operator.Weave(left, right);
         public static bool operator !=(EqualsOrSubtypeStruct left, EqualsOrSubtypeStruct right) => Operator.Weave(left, right);
     }
 }