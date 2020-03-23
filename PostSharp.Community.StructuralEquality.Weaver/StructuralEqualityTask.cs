using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Community.StructuralEquality.Weaver.Subroutines;
using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Helpers;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Compilers;
using PostSharp.Sdk.Extensibility.Configuration;
using PostSharp.Sdk.Extensibility.Tasks;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    [ExportTask(Phase = TaskPhase.Transform, TaskName = nameof(StructuralEqualityTask))] 
    [TaskDependency("AnnotationRepository", IsRequired = true, Position = DependencyPosition.Before)]
    public class StructuralEqualityTask : Task
    {
        public override bool Execute()
        {
            var annotationRepositoryService = this.Project.GetService<IAnnotationRepositoryService>();
            
            // Find ignored fields
            var ignoredFields = IgnoredFields.GetIgnoredFields(annotationRepositoryService,
                Project.GetService<ICompilerAdapterService>());

            // Sort types by inheritance hierarchy
            var toEnhance = GetTypesToEnhance(annotationRepositoryService);

            HashCodeInjection hashCodeInjection = new HashCodeInjection(this.Project);
            EqualsInjection equalsInjection = new EqualsInjection(this.Project);
            OperatorInjection operatorInjection = new OperatorInjection(this.Project);

            foreach (EqualsType enhancedTypeData in toEnhance)
            {
                var enhancedType = enhancedTypeData.EnhancedType;
                var config = enhancedTypeData.Config;
                if (!config.DoNotAddEquals)
                {
                    equalsInjection.AddEqualsTo(enhancedType, config, ignoredFields );
                }

                if ( !config.DoNotAddEqualityOperators )
                {
                    operatorInjection.ProcessEqualityOperators( enhancedType, config );
                }

                if (!config.DoNotAddGetHashCode)
                {
                    hashCodeInjection.AddGetHashCodeTo(enhancedType, config, ignoredFields);
                }
            }

            return true;
        }

        private static LinkedList<EqualsType> GetTypesToEnhance(IAnnotationRepositoryService annotationRepositoryService)
        {
            IEnumerator<IAnnotationInstance> annotationsOfType =
                annotationRepositoryService.GetAnnotationsOfType(typeof(StructuralEqualityAttribute), false, false);
            LinkedList<EqualsType> toEnhance = new LinkedList<EqualsType>();

            while (annotationsOfType.MoveNext())
            {
                IAnnotationInstance annotation = annotationsOfType.Current;
                if (annotation.TargetElement is TypeDefDeclaration enhancedType)
                {
                    TypeDefDeclaration baseClass = enhancedType.BaseTypeDef;
                    StructuralEqualityAttribute config = EqualityConfiguration.ExtractFrom(annotation.Value);
                    LinkedListNode<EqualsType> node = toEnhance.First;
                    EqualsType newType = new EqualsType(enhancedType, config);
                    while (node != null)
                    {
                        if (node.Value.EnhancedType == baseClass)
                        {
                            toEnhance.AddAfter(node, newType);
                            break;
                        }

                        node = node.Next;
                    }

                    if (node == null)
                    {
                        toEnhance.AddFirst(newType);
                    }
                }
            }

            return toEnhance;
        }
    }
}