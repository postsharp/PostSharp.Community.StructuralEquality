namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.CustomEquals
{
    [StructuralEquality]
    public class CustomGetHashCode
    {
        public int X { get; set; }

        [AdditionalGetHashCodeMethod]
        int CustomGetHashCodeMethod()
        {
            return 42;
        }

        public static bool operator ==(CustomGetHashCode left, CustomGetHashCode right) => Operator.Weave(left, right);
        public static bool operator !=(CustomGetHashCode left, CustomGetHashCode right) => Operator.Weave(left, right);
    }
}