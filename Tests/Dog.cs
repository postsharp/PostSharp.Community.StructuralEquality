using System;
using Xunit;

namespace PostSharp.Community.StructuralEquality.Tests
{
    [StructuralEquality]
    class Dog
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public static bool operator ==(Dog left, Dog right) => Operator.Weave(left, right);
        public static bool operator !=(Dog left, Dog right) => Operator.Weave(left, right);
    }

    public class DogTest
    {
        [Fact]
        public void TwoDogs()
        {
            // This is here to avoid optimization, always returns "do".
            string d = (new Random().Next(0, 1) == 0 ? "do" : "");
            string f = "Fi" + d;
            Assert.Equal(new Dog() { Name = "Fido"}, new Dog() { Name = f });
            Assert.NotEqual(new Dog() { Name = "Azor"}, new Dog() { Name = "Fido" });
        }
    }
}