using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using PostSharp.Extensibility;
using Xunit;

namespace PostSharp.Community.StructuralEquality.Tests
{
    public class ReadmeEqualityTest
    {
        [Fact]
        public void SimpleCase()
        {
            Assert.Equal(new EnhancedIntegerHolder(2,2),  new EnhancedIntegerHolder(2,2));
        }
        [Fact]
        public void ControlCase()
        {
            Assert.NotEqual(new NotEnhancedIntegerHolder(2,2),  new NotEnhancedIntegerHolder(2,2));
        }
    }
    
    [StructuralEquality(DoNotAddEqualityOperators = true)]
    public class EnhancedIntegerHolder
    {
        public int X { get; }
        public int Y { get; }
        
        public EnhancedIntegerHolder( int x, int y )
        {
            X = x;
            Y = y;
        }
    }
    public class NotEnhancedIntegerHolder
    {
        public int X { get; }
        public int Y { get; }
        
        public NotEnhancedIntegerHolder( int x, int y )
        {
            X = x;
            Y = y;
        }
    }
}