namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
{
    [StructuralEquality]
    public class ClassWithGenericBase :
        GenericBase<int>
    {
        public int Prop { get; set; }

        public static bool operator ==(ClassWithGenericBase left, ClassWithGenericBase right) => Operator.Weave(left, right);
        public static bool operator !=(ClassWithGenericBase left, ClassWithGenericBase right) => Operator.Weave(left, right);
    }
}