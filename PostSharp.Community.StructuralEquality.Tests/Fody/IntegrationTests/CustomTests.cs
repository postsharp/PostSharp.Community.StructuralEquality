using System;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.CustomEquals;
using Xunit;

namespace PostSharp.Community.StructuralEquality.Tests.Fody.IntegrationTests
{
    public class CustomTests
    {
        [Fact]
        public void Equals_should_use_custom_logic()
        {
            var first = new CustomEquals();
            first.X = 1;

            var second = new CustomEquals();
            second.X = 2;

            var result = first.Equals(second);

            Assert.True(result);
            Assert.True( first.CustomCalled );
        }

        [Fact]
        public void Equals_should_use_custom_logic_for_structure()
        {
            var first = new CustomStructEquals();
            first.X = 1;

            var second = new CustomStructEquals();
            second.X = 2;

            var result = first.Equals(second);

            Assert.True(result);
            Assert.True( first.CustomCalled );
        }

        [Fact]
        public void Equals_should_use_custom_logic_for_generic_type()
        {
            var first = new CustomGenericEquals<int>();
            first.Prop = 1;
            var second = new CustomGenericEquals<int>();
            second.Prop = 1;
            var third = new CustomGenericEquals<int>();
            third.Prop = 2;

            Assert.True(first.Equals(second));
            Assert.False(first.Equals(third));
            Assert.True( first.CustomCalled );
        }

        [Fact]
        public void GetHashCode_should_use_custom_logic()
        {
            var instance = new CustomGetHashCode();
            instance.X = 1;

            var result = instance.GetHashCode();

            Assert.Equal(423, result);
        }

        [Fact]
        public void GetHashCode_should_use_custom_logic_for_structure()
        {
            var instance = new CustomStructEquals();
            instance.X = 1;

            var result = instance.GetHashCode();

            Assert.Equal(42, result);
        }

        [Fact]
        public void GetHashCode_should_use_custom_logic_for_generic_type()
        {
            var instance = new CustomGenericEquals<int>();
            instance.Prop = 1;

            var result = instance.GetHashCode();

            Assert.Equal(42, result);
        }
    }
}