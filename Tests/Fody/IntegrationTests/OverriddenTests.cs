using System;
using System.Linq;
using System.Threading.Tasks;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.GenericClass;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.OverridenWithIgnoredProperties;
using Xunit;

namespace PostSharp.Community.StructuralEquality.Tests.Fody.IntegrationTests
{
    public class OverriddenTests
    {
        [Theory]
        [InlineData("123", "123")]
        [InlineData("123", "456")]
        public void Equals_should_ignore_marked_overridden_properties(string location1, string location2)
        {
            var first = new ProjectClass();
            first.Location = location1;
            first.X = 42;

            var second = new ProjectClass();
            second.Location = location2;
            second.X = 42;

            Assert.Equal(first, second);
        }
    }
}