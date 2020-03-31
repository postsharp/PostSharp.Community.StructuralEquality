using PostSharp.Community.StructuralEquality;

namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
{
    [StructuralEquality(TypeCheck = TypeCheck.ExactlyTheSameTypeAsThis)]
    public class ExactlyTheSameTypeAsThisStruct
    {
        public int A { get; set; }

        public static bool operator ==(ExactlyTheSameTypeAsThisStruct left, ExactlyTheSameTypeAsThisStruct right) => Operator.Weave(left, right);
        public static bool operator !=(ExactlyTheSameTypeAsThisStruct left, ExactlyTheSameTypeAsThisStruct right) => Operator.Weave(left, right);
    }
}