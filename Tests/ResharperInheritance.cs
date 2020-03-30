namespace PostSharp.Community.StructuralEquality.Tests
{
    public class ResharperInheritance
    {
        
    }

    public class ABase
    {
        public int Alpha { get; set; }
    }

    public class ASubclass : ABase
    {
        public int Beta { get; set; }

        protected bool Equals(ASubclass other)
        {
            return Beta == other.Beta;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ASubclass other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Beta;
        }
    }
}