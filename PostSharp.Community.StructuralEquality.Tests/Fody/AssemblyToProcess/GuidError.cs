namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
{
    [StructuralEquality]
    public class ReferenceObject :
        NameObject
    {
        public static bool operator ==(ReferenceObject left, ReferenceObject right) => Operator.Weave(left, right);
        public static bool operator !=(ReferenceObject left, ReferenceObject right) => Operator.Weave(left, right);
    }
}