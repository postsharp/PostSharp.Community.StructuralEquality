using System.Collections.Generic;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Compilers;
using PostSharp.Sdk.Extensibility.Tasks;

namespace PostSharp.Community.StructuralEquality.Weaver.Subroutines
{
    public static class IgnoredFields
    {
        /// <summary>
        /// Gets the set of all fields that should not participate in Equals and GetHashCode generated because they're
        /// the target of [IgnoreDuringEquals].
        /// </summary>
        public static ISet<FieldDefDeclaration> GetIgnoredFields(IAnnotationRepositoryService annotations, 
            ICompilerAdapterService compilerAdapterService)
        {
            HashSet<FieldDefDeclaration> fields = new HashSet<FieldDefDeclaration>();
            IEnumerator<IAnnotationInstance> ignoredFieldsAnnotations = 
                annotations.GetAnnotationsOfType( typeof(IgnoreDuringEqualsAttribute), false, false );
            while (ignoredFieldsAnnotations.MoveNext())
            {
                IAnnotationInstance annotationInstance = ignoredFieldsAnnotations.Current;
                MetadataDeclaration targetElement = annotationInstance.TargetElement;
                if (targetElement is FieldDefDeclaration field)
                {
                    fields.Add(field);
                }
                else if (targetElement is PropertyDeclaration propertyDeclaration)
                {
                    FieldDefDeclaration backingField = compilerAdapterService.GetBackingField(propertyDeclaration);
                    if ( backingField != null )
                    {
                        fields.Add( backingField );
                    }
                    else
                    {
                        // The property is not an automatic property.
                        // It's ignored, because there is no backing field and we make equality by fields.
                        // We could emit a warning but I don't think that's a great idea. Like, yeah, ignoring a 
                        // non-automatic property has no effect but so what: it's ignored, just like the user wanted.
                    }
                }
            }

            return fields;
        } 
    }
}