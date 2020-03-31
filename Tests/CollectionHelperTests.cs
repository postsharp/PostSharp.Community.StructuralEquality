using System;
using System.Collections.Generic;
using FakeItEasy;
using Xunit;

namespace PostSharp.Community.StructuralEquality.Tests
{
    public class CollectionHelperTests
    {
        [Fact]
        public void Nulls()
        {
            Assert.True(CollectionHelper.Equals(null, null));
            Assert.False(CollectionHelper.Equals(new int[0], null));
            Assert.False(CollectionHelper.Equals(null, new int[0]));
            Assert.True(CollectionHelper.Equals(new int[0], new int[0]));
        }

        [Fact]
        public void ArrayWithNull()
        {
            Assert.False(CollectionHelper.Equals(new string[] { null }, new string[] { "hello "}));
            Assert.True(CollectionHelper.Equals(new string[] { null }, new string[] { null }));
        }
        
        [Fact]
        public void ArrayWithDifferentSizes()
        {
            Assert.False(CollectionHelper.Equals(new string[] { "hello ", "hi" }, new string[] { "hello "}));
            Assert.False(CollectionHelper.Equals(new string[] { "hi" }, new string[] { "hello" }));
        }

        [Fact]
        public void CollectionUsesCount()
        {
            var oneCollection = A.Fake<ICollection<string>>();
            A.CallTo(() => oneCollection.Count).Returns(2);
            A.CallTo(() => oneCollection.GetEnumerator()).Throws<Exception>();
            var str = new string[3];
            Assert.False(CollectionHelper.Equals(oneCollection, str));
        }
    }
}