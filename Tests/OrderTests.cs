using Xunit;

namespace PostSharp.Community.StructuralEquality.Tests
{
    public class OrderTests
    {
        [Fact]
        public void TestOrder()
        {
            // tests that types are processed in inheritance order
            Assert.Equal(new A_New() { b = 3 }, new A_New() { b = 3});
            Assert.NotEqual(new A_New() { b = 3 }, new A_New() { b = 4});
        }
    }
    
    [StructuralEquality(DoNotAddEqualityOperators = true)]
    public class A_New : B_Old
    {

    }

    [StructuralEquality(DoNotAddEqualityOperators = true)]
    public class B_Old
    {
        public int b;
    }
}