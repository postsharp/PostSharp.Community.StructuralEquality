using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Community.StructuralEquality.Weaver.Subroutines;
using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;
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
                try
                {
                    if ( !config.DoNotAddEquals )
                    {
                        equalsInjection.AddEqualsTo( enhancedType, config, ignoredFields );
                    }

                    if ( !config.DoNotAddEqualityOperators )
                    {
                        operatorInjection.ProcessEqualityOperators( enhancedType, config );
                    }

                    if ( !config.DoNotAddGetHashCode )
                    {
                        hashCodeInjection.AddGetHashCodeTo( enhancedType, config, ignoredFields );
                    }
                }
                catch ( InjectionException exception )
                {
                    Message.Write( enhancedType, SeverityType.Error, exception.ErrorCode, exception.Message );
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets types for which Equals or GetHashCode should be synthesized, in such an order that base classes come before
        /// derived classes. This way, when Equals for a derived class is being created, you can be already sure that
        /// the Equals for the base class was already created (if the base class was target of [StructuralEquality].
        /// </summary>
        private List<EqualsType> GetTypesToEnhance()
        {
            IEnumerator<IAnnotationInstance> annotationsOfType =
                annotationRepositoryService.GetAnnotationsOfType(typeof(StructuralEqualityAttribute), false, false);

            List<EqualsType> toEnhance = new List<EqualsType>();
            
            while (annotationsOfType.MoveNext())
            {
                IAnnotationInstance annotation = annotationsOfType.Current;
                if (annotation?.TargetElement is TypeDefDeclaration enhancedType)
                {
                    StructuralEqualityAttribute config = EqualityConfiguration.ExtractFrom(annotation.Value);
                    EqualsType newType = new EqualsType(enhancedType, config);
                    toEnhance.Add( newType );
                }
            }
            
            toEnhance.Sort( ( first, second ) =>
            {
                if ( first.EnhancedType.IsAssignableTo( second.EnhancedType ) )
                {
                    if ( second.EnhancedType.IsAssignableTo( first.EnhancedType ) )
                    {
                        return 0;
                    }

                    return 1;
                }
                
                return -1;
            });

            return toEnhance;
        }
    }
}