namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
{
     [StructuralEquality(TypeCheck = TypeCheck.ExactlyOfType)]
     public struct ExactlyOfTypeStruct
     {
         public int A { get; set; }

         public static bool operator ==(ExactlyOfTypeStruct left, ExactlyOfTypeStruct right) => Operator.Weave(left, right);
         public static bool operator !=(ExactlyOfTypeStruct left, ExactlyOfTypeStruct right) => Operator.Weave(left, right);
     }
}