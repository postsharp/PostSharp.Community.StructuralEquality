using System.Xml;
using Xunit;

namespace PostSharp.Community.StructuralEquality.Tests
{
    public class WithReSharperControlTest
    {
        [Fact]
        public void EnhancedCase()
        {
            EnhancedCase a = new EnhancedCase { a = 2, B = "Hello", C = SomeType.Instance };
            EnhancedCase b = new EnhancedCase { a = 2, B = "Hello", C = SomeType.Instance };
            Assert.Equal(a, b);
        }
        [Fact]
        public void ControlCase()
        {
            ControlCase a = new ControlCase { a = 2, B = "Hello", C = SomeType.Instance };
            ControlCase b = new ControlCase { a = 2, B = "Hello", C = SomeType.Instance };
            Assert.NotEqual(a, b);
        }
        
        [Fact]
        public void ResharperCase()
        {
            ReSharperCreated a = new ReSharperCreated { a = 2, B = "Hello", C = SomeType.Instance };
            ReSharperCreated b = new ReSharperCreated { a = 2, B = "Hello", C = SomeType.Instance };
            Assert.Equal(a, b);
        }
    }

    [StructuralEquality(DoNotAddEqualityOperators = true)]
    public class EnhancedCase
    {
        public int a;
        public string B { get; set; }
        public SomeType C { get; set;  }
    }

    public class ControlCase
    {
        public int a;
        public string B { get; set; }
        public SomeType C { get; set; }
    }

    public class SomeType
    {
        public static SomeType Instance { get; } = new SomeType();
    }

    public class ReSharperCreated
    {
        public int a;
        public string B { get; set; }
        public SomeType C { get; set; }

        protected bool Equals(ReSharperCreated other)
        {
            return a == other.a && B == other.B && Equals(C, other.C);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ReSharperCreated) obj);
        }
        
        

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 0 ^ a;
                hashCode = (hashCode * 397) ^ (B != null ? B.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (C != null ? C.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}