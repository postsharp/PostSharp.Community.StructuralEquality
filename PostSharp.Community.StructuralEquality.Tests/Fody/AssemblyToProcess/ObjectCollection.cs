using System.Collections.Generic;

namespace PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess
{
    [StructuralEquality]
    public class ObjectCollection
    {
        public IEnumerable<object> Collection { get; set; }

        public static bool operator ==(ObjectCollection left, ObjectCollection right) => Operator.Weave(left, right);
        public static bool operator !=(ObjectCollection left, ObjectCollection right) => Operator.Weave(left, right);
    }
}