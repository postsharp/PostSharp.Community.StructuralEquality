﻿﻿ namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.CustomEquals
 {
     [StructuralEquality]
     public class CustomEquals
     {
         [IgnoreDuringEquals]
         public int X { get; set; }

         [IgnoreDuringEquals]
         public bool CustomCalled { get; set; }

         [AdditionalEqualsMethod]
         bool CustomEqualsMethod(CustomEquals other)
         {
             this.CustomCalled = true;
             return X == 1 && other.X == 2 || X == 2 && other.X == 1;
         }

         [AdditionalGetHashCodeMethod]
         int CustomGetHashCode()
         {
             return 42;
         }

         public static bool operator ==(CustomEquals left, CustomEquals right) => Operator.Weave(left, right);
         public static bool operator !=(CustomEquals left, CustomEquals right) => Operator.Weave(left, right);
     }
 }