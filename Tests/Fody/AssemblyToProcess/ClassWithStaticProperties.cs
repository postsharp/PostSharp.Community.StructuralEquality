﻿﻿ namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
 {
     [StructuralEquality]
     public class ClassWithStaticProperties
     {
         public int X { get; set; }

         public string Y { get; set; }

         public static double Z { get; set; }

         public static char V { get; set; }

         public static bool operator ==(ClassWithStaticProperties left, ClassWithStaticProperties right) => Operator.Weave(left, right);
         public static bool operator !=(ClassWithStaticProperties left, ClassWithStaticProperties right) => Operator.Weave(left, right);
     }
 }