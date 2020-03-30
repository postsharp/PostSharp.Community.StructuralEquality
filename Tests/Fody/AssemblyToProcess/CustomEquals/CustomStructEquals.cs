namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.CustomEquals
{
    [StructuralEquality]
    public struct CustomStructEquals
    {
        [IgnoreDuringEquals]
        public int X { get; set; }
        
        [IgnoreDuringEquals]
        public bool CustomCalled { get; set; }

        [AdditionalEqualsMethod]
        bool CustomEquals(CustomStructEquals other)
        {
            this.CustomCalled = true;
            return X == 1 && other.X == 2 || X == 2 && other.X == 1;
        }

        [AdditionalGetHashCodeMethod]
        int CustomGetHashCode()
        {
            return 42;
        }

        public static bool operator ==(CustomStructEquals left, CustomStructEquals right) => Operator.Weave(left, right);
        public static bool operator !=(CustomStructEquals left, CustomStructEquals right) => Operator.Weave(left, right);
    }
}