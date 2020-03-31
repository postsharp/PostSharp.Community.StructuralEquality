using PostSharp.Community.StructuralEquality;

namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
{
    [StructuralEquality(TypeCheck = TypeCheck.ExactlyTheSameTypeAsThis)]
    public class ExactlyTheSameTypeAsThisClass
    {
        public int A { get; set; }

        public static bool operator ==(ExactlyTheSameTypeAsThisClass left, ExactlyTheSameTypeAsThisClass right) => Operator.Weave(left, right);
        public static bool operator !=(ExactlyTheSameTypeAsThisClass left, ExactlyTheSameTypeAsThisClass right) => Operator.Weave(left, right);
    }

    public class ExactlyTheSameTypeAsThisSubClass : ExactlyTheSameTypeAsThisClass
    {
        public int B { get; set; }
    }
}