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
                    while ( true )
                    {
                        FieldDefDeclaration backingField = compilerAdapterService.GetBackingField(propertyDeclaration);
                        if ( backingField != null )
                        {
                            fields.Add( backingField );
                        }

                        if ( !propertyDeclaration.Getter.IsVirtual )
                        {
                            break;
                        }
                        
                        propertyDeclaration = propertyDeclaration.Parent.BaseTypeDef.FindProperty( propertyDeclaration.Name )?.Property;
                        if ( propertyDeclaration == null )
                        {
                            break;
                        }
                    }
                }
            }

            return fields;
        } 
    }
}