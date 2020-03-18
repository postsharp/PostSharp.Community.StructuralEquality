using PostSharp.Sdk.CodeModel;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    public class EqualsType
    {
        public TypeDefDeclaration EnhancedType { get; }
        public StructuralEqualityAttribute Config { get; }

        public EqualsType(TypeDefDeclaration enhancedType, StructuralEqualityAttribute config)
        {
            EnhancedType = enhancedType;
            Config = config;
        }
    }
}