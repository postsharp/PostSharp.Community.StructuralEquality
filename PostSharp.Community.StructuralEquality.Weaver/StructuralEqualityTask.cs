using System;
using System.Collections.Generic;
using System.Reflection;
using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.Extensibility;
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
            IEnumerator<IAnnotationInstance> annotationsOfType = annotationRepositoryService.GetAnnotationsOfType( typeof(StructuralEqualityAttribute), false, false );
            while ( annotationsOfType.MoveNext() )
            {
                IAnnotationInstance annotation = annotationsOfType.Current;
                if ( annotation.TargetElement is TypeDefDeclaration enhancedType )
                {
                    this.AddEqualsAndGetHashCodeTo( enhancedType );
                }
            }
            return true;
        }

        private void AddEqualsAndGetHashCodeTo(TypeDefDeclaration enhancedType)
        {            
            this.AddEqualsTo( enhancedType );
            this.AddGetHashCodeTo( enhancedType );
        }
        
        private void AddEqualsTo( TypeDefDeclaration enhancedType )
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

        private void AddGetHashCodeTo( TypeDefDeclaration enhancedType )
        {
            // TODO test for existing GetHashCode and do nothing if it's present
            
            // Create signature
            MethodDefDeclaration getHashCode = new MethodDefDeclaration
                                               {
                                                   Name = "GetHashCode",
                                                   CallingConvention = CallingConvention.HasThis,
                                                   Attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig
                                               };
            enhancedType.Methods.Add( getHashCode );
            getHashCode.ReturnParameter = ParameterDeclaration.CreateReturnParameter( enhancedType.Module.Cache.GetIntrinsic( IntrinsicType.Int32 ) );
            
            // Generate ReSharper-style Fowler–Noll–Vo hash:
            using ( InstructionWriter writer = InstructionWriter.GetInstance() )
            {
                CreatedEmptyMethod getHashCodeData = MethodBodyCreator.CreateModifiableMethodBody( writer, getHashCode );
                writer.AttachInstructionSequence( getHashCodeData.PrincipalBlock.AddInstructionSequence(  ) );
                // Start with 0
                // TODO add base type's hash code
                writer.EmitInstruction( OpCodeNumber.Ldc_I4_0 );

                // For each field, do "hash = hash * 397 ^ field?.GetHashCode();
                foreach ( FieldDefDeclaration field in enhancedType.Fields )
                {
                    if ( field.IsConst || field.IsStatic )
                    {
                        continue;
                    }
                    writer.EmitInstructionInt32( OpCodeNumber.Ldc_I4, 397 );
                    writer.EmitInstruction( OpCodeNumber.Mul );
                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 ); // TODO what if I am a struct?
                    writer.EmitInstructionField( OpCodeNumber.Ldfld, field );
                    // TODO check for null
                    // TODO GetHashCode(), actually
                    // TODO what if the field is a struct?
                    writer.EmitInstruction( OpCodeNumber.Xor );
                }
                
                // Return the hash:
                writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, getHashCodeData.ReturnVariable );
                writer.EmitBranchingInstruction( OpCodeNumber.Br, getHashCodeData.ReturnSequence );
                writer.DetachInstructionSequence();
            }
        }
    }
}