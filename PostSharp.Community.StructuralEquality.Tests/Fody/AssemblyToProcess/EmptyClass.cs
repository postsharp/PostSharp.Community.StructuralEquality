﻿﻿ namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
 {
     [StructuralEquality]
     public class EmptyClass
     {
         public static bool operator ==(EmptyClass left, EmptyClass right) => Operator.Weave(left, right);
         public static bool operator !=(EmptyClass left, EmptyClass right) => Operator.Weave(left, right);
     }
 }