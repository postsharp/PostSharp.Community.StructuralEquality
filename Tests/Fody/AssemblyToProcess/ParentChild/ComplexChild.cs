using System.Collections.Generic;

namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.ParentChild
{
    [StructuralEquality]
    public class ComplexChild :
        ComplexParent
    {
        public long InChildNumber { get; set; }

        public string InChildText { get; set; }

        public IEnumerable<int> InChildCollection { get; set; }

        public static bool operator ==(ComplexChild left, ComplexChild right) => Operator.Weave(left, right);
        public static bool operator !=(ComplexChild left, ComplexChild right) => Operator.Weave(left, right);
    }
}