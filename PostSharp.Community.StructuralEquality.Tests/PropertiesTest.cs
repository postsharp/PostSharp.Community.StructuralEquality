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
            A a  = new A { Property = 2 };
            A b  = new A { Property = 2 };
            Assert.False(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }
        [Fact]
        public void TestDoNotAddHashCode()
        {
            B a  = new B { Property = 2 };
            B b  = new B { Property = 2 };
            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), RuntimeHelpers.GetHashCode(a));
        }
    }

    [StructuralEquality(DoNotAddEquals = true)]
    public class A
    {
        public int Property { get; set; }
    }
    [StructuralEquality(DoNotAddGetHashCode = true)]
    public class B
    {
        public int Property { get; set; }
    }
}