﻿ namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
 {
     [StructuralEquality(TypeCheck = TypeCheck.SameTypeOrSubtype)]
     public struct SameTypeOrSubtypeStruct
     {
         public int A { get; set; }

         public static bool operator ==(SameTypeOrSubtypeStruct left, SameTypeOrSubtypeStruct right) => Operator.Weave(left, right);
         public static bool operator !=(SameTypeOrSubtypeStruct left, SameTypeOrSubtypeStruct right) => Operator.Weave(left, right);
     }
 }