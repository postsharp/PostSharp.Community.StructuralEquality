using System.Collections;
using PostSharp.Sdk.CodeModel;

namespace PostSharp.Community.StructuralEquality.Weaver.Subroutines
{
    public static class Extensions
    {
        public static bool IsCollection(this ITypeSignature type)
        {
            return type.IsAssignableToRuntimeType(typeof(IEnumerable)) &&
                   !type.IsAssignableToRuntimeType(typeof(string));
            // return false;
            // !type..Equals("String") &&
            //        type.Interfaces.Any(i => i.InterfaceType.Name.Equals("IEnumerable"));
        }
    }
}