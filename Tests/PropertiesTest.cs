using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace PostSharp.Community.StructuralEquality.Tests
{
    public class PropertiesTest
    {
        [Fact]
        public void TestDoNotAddEquals()
        {
            PAA paa  = new PAA { Property = 2 };
            PAA b  = new PAA { Property = 2 };
            Assert.False(paa.Equals(b));
            Assert.Equal(paa.GetHashCode(), b.GetHashCode());
        }
        [Fact]
        public void TestDoNotAddHashCode()
        {
            PBB a  = new PBB { Property = 2 };
            PBB pbb  = new PBB { Property = 2 };
            Assert.True(a.Equals(pbb));
            Assert.Equal(a.GetHashCode(), RuntimeHelpers.GetHashCode(a));
        }
    }

    [StructuralEquality(DoNotAddEquals = true, DoNotAddEqualityOperators = true)]
    public class PAA
    {
        public int Property { get; set; }
    }
    [StructuralEquality(DoNotAddGetHashCode = true, DoNotAddEqualityOperators = true)]
    public class PBB
    {
        public int Property { get; set; }
    }
}