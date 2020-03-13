using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Collections;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    public class EqualityConfiguration
    {
        public static StructuralEqualityAttribute ExtractFrom(IAnnotationValue trueAttribute)
        {
            StructuralEqualityAttribute equality = new StructuralEqualityAttribute();
            MemberValuePairCollection namedArguments = trueAttribute.NamedArguments;
            equality.DoNotAddEquals = (bool)(namedArguments[nameof(StructuralEqualityAttribute.DoNotAddEquals)]?.Value.Value ?? false);
            equality.DoNotAddGetHashCode = (bool)(namedArguments[nameof(StructuralEqualityAttribute.DoNotAddGetHashCode)]?.Value.Value ?? false);
            return equality;
        }
    }
}