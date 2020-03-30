using System;
using System.Collections.Generic;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable 169

namespace PostSharp.Community.StructuralEquality.Tests
{
    [StructuralEquality(TypeCheck = TypeCheck.ExactlyTheSameTypeAsThis)]
    public class AdvancedCase : AdvancedBaseClass
    {
        protected string field;
        public List<List<object>> lists { get; }= new List<List<object>>();
        [IgnoreDuringEquals]
        public float DoNotUse { get; set; }

        [AdditionalEqualsMethod]
        public bool AndFloatWithinRange(AdvancedCase other)
        {
            return Math.Abs(this.DoNotUse - other.DoNotUse) < 0.1f;
        }
        
        public static bool operator ==(AdvancedCase left, AdvancedCase right) => Operator.Weave(left, right);
        public static bool operator !=(AdvancedCase left, AdvancedCase right) => Operator.Weave(left, right);

    }

    [StructuralEquality(DoNotAddEqualityOperators = true, DoNotAddGetHashCode = true)]
    public class AdvancedBaseClass
    {
        private int baseField;
    }
}
