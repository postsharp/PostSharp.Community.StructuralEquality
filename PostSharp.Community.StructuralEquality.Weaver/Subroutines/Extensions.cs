using System.Collections;
using PostSharp.Sdk.CodeModel;

namespace PostSharp.Community.StructuralEquality.Weaver.Subroutines
{
    public static class Extensions
    {
        public static bool IsCollection(this ITypeSignature type)
        {
            var stringType = type.Module.Cache.GetIntrinsic( IntrinsicType.String );
            var iEnumerableType = type.Module.Cache.GetType( typeof(IEnumerable) );
            return !type.IsAssignableTo( stringType ) && type.IsAssignableTo( iEnumerableType );
        }
    }
}