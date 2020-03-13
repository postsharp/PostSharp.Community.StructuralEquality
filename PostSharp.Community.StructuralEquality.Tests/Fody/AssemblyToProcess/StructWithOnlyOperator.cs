﻿﻿ namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
 {
     [StructuralEquality(DoNotAddEquals = true, DoNotAddGetHashCode = true)]
     public struct StructWithOnlyOperator
     {
         public int Value { get; set; }

         public override bool Equals(object obj)
         {
             var second = (StructWithOnlyOperator)obj;

             return Value == 1 && second.Value == 2;
         }

         public static bool operator ==(StructWithOnlyOperator left, StructWithOnlyOperator right) => Operator.Weave(left, right);
         public static bool operator !=(StructWithOnlyOperator left, StructWithOnlyOperator right) => Operator.Weave(left, right);
     }
 }