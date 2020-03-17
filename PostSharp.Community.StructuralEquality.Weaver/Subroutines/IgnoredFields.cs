using System.Collections.Generic;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Compilers;
using PostSharp.Sdk.Extensibility.Tasks;

namespace PostSharp.Community.StructuralEquality.Weaver.Subroutines
{
    public static class IgnoredFields
    {
        public static ISet<FieldDefDeclaration> GetIgnoredFields(IAnnotationRepositoryService annotations, ICompilerAdapterService compilerAdapterService)
        {
            HashSet<FieldDefDeclaration> fields = new HashSet<FieldDefDeclaration>();
            IEnumerator<IAnnotationInstance> ignoredFieldsAnnotations = annotations.GetAnnotationsOfType( typeof(IgnoreDuringEqualsAttribute), false, false );
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
                    if (backingField != null)
                    {
                        fields.Add(backingField);
                    }
                }
            }

            return fields;
        } 
    }
}