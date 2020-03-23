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

            foreach (EqualsType enhancedTypeData in toEnhance)
            {
                var enhancedType = enhancedTypeData.EnhancedType;
                var config = enhancedTypeData.Config;
                if (!config.DoNotAddEquals)
                {
                    this.AddEqualsTo(enhancedType, config);
                }

                if (!config.DoNotAddGetHashCode)
                {
                    hashCodeInjection.AddGetHashCodeTo(enhancedType, config, ignoredFields);
                }

                // TODO implement operators
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

        private void AddEqualsTo(TypeDefDeclaration enhancedType, StructuralEqualityAttribute config)
        {            
            // TODO test for existing Equals and do nothing if it's present
            // Create signature
            MethodDefDeclaration equalsDeclaration = new MethodDefDeclaration
                                               {
                                                   Name = "Equals",
                                                   CallingConvention = CallingConvention.HasThis,
                                                   Attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig
                                               };
            enhancedType.Methods.Add( equalsDeclaration );
            equalsDeclaration.Parameters.Add( new ParameterDeclaration( 0, "other", enhancedType.Module.Cache.GetIntrinsic( IntrinsicType.Object )  ));
            equalsDeclaration.ReturnParameter = ParameterDeclaration.CreateReturnParameter( enhancedType.Module.Cache.GetIntrinsic( IntrinsicType.Boolean ) );
            

            // Generate ReSharper-style Equals comparison:
            using ( InstructionWriter writer = InstructionWriter.GetInstance() )
            {
                CreatedEmptyMethod equals = MethodBodyCreator.CreateModifiableMethodBody( writer, equalsDeclaration );
                LocalVariableSymbol afterCast = @equals.PrincipalBlock.DefineLocalVariable( enhancedType, "otherAfterCast" );
                writer.AttachInstructionSequence( equals.PrincipalBlock.AddInstructionSequence() );
                // TODO if (other == null) return false;
                // TODO if (this == other) return true;
                writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
                writer.EmitInstructionType( OpCodeNumber.Isinst, enhancedType );
                writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, afterCast );
                // TODO if (topOfStack == null) return false;
                
                // For each field, do "if (!this.field?.Equals(otherAfterCast.field)) return false;"
                foreach ( FieldDefDeclaration field in enhancedType.Fields )
                {
                    if ( field.IsConst || field.IsStatic )
                    {
                        continue;
                    }

                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 ); // TODO what if I am a struct?
                    writer.EmitInstructionField( OpCodeNumber.Ldfld, field );
                    writer.EmitInstructionLocalVariable( OpCodeNumber.Ldloc, afterCast ); // TODO what if they are a struct?
                    writer.EmitInstructionField( OpCodeNumber.Ldfld, field ); 
                    // TODO check for null
                    // TODO Equals(), actually
                    // TODO what if the field is a struct?
                    writer.EmitInstruction( OpCodeNumber.Ceq );
                    writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, equals.ReturnSequence );
                }

                // If we go here, return true.
                writer.EmitInstruction( OpCodeNumber.Ldc_I4_1 );
                writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, equals.ReturnVariable );
                writer.EmitBranchingInstruction( OpCodeNumber.Br, equals.ReturnSequence );
                writer.DetachInstructionSequence();
            }
        }
    }
}