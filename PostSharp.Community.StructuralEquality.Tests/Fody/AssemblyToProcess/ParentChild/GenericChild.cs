namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.ParentChild
{
    [StructuralEquality]
    public class GenericChild :
        GenericParent<int>
    {
        public string InChild { get; set; }

        public static bool operator ==(GenericChild left, GenericChild right) => Operator.Weave(left, right);
        public static bool operator !=(GenericChild left, GenericChild right) => Operator.Weave(left, right);
    }
}