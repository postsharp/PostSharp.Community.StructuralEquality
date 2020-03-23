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
        [ImportService] 
        private IAnnotationRepositoryService annotationRepositoryService;
        
        public override bool Execute()
        {
            // Find ignored fields
            var ignoredFields = IgnoredFields.GetIgnoredFields(annotationRepositoryService,
                Project.GetService<ICompilerAdapterService>());

            // Sort types by inheritance hierarchy
            var toEnhance = this.GetTypesToEnhance();

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

        /// <summary>
        /// Gets types for which Equals or GetHashCode should be synthesized, in such an order that base classes come before
        /// derived classes. This way, when Equals for a derived class is being created, you can be already sure that
        /// the Equals for the base class was already created (if the base class was target of [StructuralEquality].
        /// </summary>
        private LinkedList<EqualsType> GetTypesToEnhance()
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