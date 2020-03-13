namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
{
    public class PropertyAttributesWithNoEquals
    {
        [IgnoreDuringEquals]
        public int Property { get; set; }

        [AdditionalEqualsMethod]
        [AdditionalGetHashCodeMethod]
        public void Method()
        {
        }
    }
}