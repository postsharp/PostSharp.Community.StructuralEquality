using Xunit;
#pragma warning disable 414

namespace PostSharp.Community.StructuralEquality.Tests.GetHashCode
{
    public class SpecialCasesTests
    {
        [Fact]
        public void TestDoNotOverwrite()
        {
            Assert.Equal(42, new HashCodeAlreadyImplemented().GetHashCode());
        }

        [Fact]
        public void MultipleCustom()
        {
            Multi one = new Multi();
            Multi two = new Multi();
            Assert.Equal(one.GetHashCode(), two.GetHashCode());
            Assert.NotEqual(0, one.GetHashCode());
            Assert.NotEqual(2, one.GetHashCode());
        }

        [StructuralEquality(DoNotAddEqualityOperators = true)]
        public class Multi
        {
            public int H { get; } = 2;

            [AdditionalGetHashCodeMethod]
            public int Oi()
            {
                return 3;
            }

            [AdditionalGetHashCodeMethod]
            public int Uk()
            {
                return 5;
            }
        }
        
        [StructuralEquality(DoNotAddEqualityOperators = true)]
        private class HashCodeAlreadyImplemented
        {
            private int aField = 21;
            public override int GetHashCode()
            {
                return 42;
            }
        }
    }
}