using System;
using System.Collections.Generic;
using System.Reflection;
using PostSharp.Community.StructuralEquality.Weaver.Subroutines;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.Extensibility;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    
    
    
    public class EqualsInjection
    {
        private readonly IGenericMethodDefinition referenceEqualsMethod;
        private readonly Project project;
        private readonly TypeDefDeclaration objectType;
        private readonly ITypeSignature booleanType;
        private readonly IGenericMethodDefinition getTypeMethod;
        private readonly IGenericMethodDefinition instanceEqualsMethod;
        private readonly IGenericMethodDefinition staticEqualsMethod;

        public EqualsInjection(Project project)
        {
            this.project = project;
            
            this.objectType = this.project.Module.Cache.GetIntrinsic(IntrinsicType.Object).GetTypeDefinition();
            this.booleanType = this.project.Module.Cache.GetIntrinsic( IntrinsicType.Boolean );
            
            this.referenceEqualsMethod = this.project.Module.FindMethod( this.objectType, "ReferenceEquals" );
            this.instanceEqualsMethod = this.project.Module.FindMethod( this.objectType, "Equals", 1 );
            this.staticEqualsMethod = this.project.Module.FindMethod( this.objectType, "Equals", 2 );
            this.getTypeMethod = this.project.Module.FindMethod( this.objectType, "GetType" );
        }
        
        public void AddEqualsTo( TypeDefDeclaration enhancedType, StructuralEqualityAttribute config,
            ISet<FieldDefDeclaration> ignoredFields )
        {
            var typedEqualsMethod = this.InjectEqualsType(enhancedType, config, ignoredFields );
            // this.InjectEqualsObject(enhancedType, config, typedEqualsMethod);
        }

        private MethodDefDeclaration InjectEqualsType( TypeDefDeclaration enhancedType,
            StructuralEqualityAttribute config, ISet<FieldDefDeclaration> ignoredFields )
        {
            // public virtual bool Equals( Base other )
            var equalsDeclaration = new MethodDefDeclaration
            {
                Name = "Equals",
                Attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                CallingConvention = CallingConvention.HasThis
            };
            enhancedType.Methods.Add( equalsDeclaration );
            equalsDeclaration.Parameters.Add( new ParameterDeclaration( 0, "other", enhancedType ));
            equalsDeclaration.ReturnParameter = ParameterDeclaration.CreateReturnParameter( this.booleanType );
            
            using ( var writer = InstructionWriter.GetInstance() )
            {
                var methodBody = MethodBodyCreator.CreateModifiableMethodBody( writer, equalsDeclaration );
                InstructionSequence instructionSequence = methodBody.PrincipalBlock.AddInstructionSequence();
                instructionSequence.Comment = "Equals(Typed other)";
                writer.AttachInstructionSequence( instructionSequence );
                
                this.InjectReferenceEquals( writer, methodBody, enhancedType );
                // Writer is either attached to the same sequence (value types) or to a new one which should check the structure.
                
                // return base.Equals(other) && this.field1 == other.field1 && ...;
                // Find the base method.

                if ( !enhancedType.IsValueType() )
                {
                    var baseEqualsMethod = this.instanceEqualsMethod.FindOverride( enhancedType.BaseTypeDef, true ).Method;
                    
                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
                    writer.EmitInstructionMethod( OpCodeNumber.Call, baseEqualsMethod );
                    
                    // base.Equals(other) returned false.
                    writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
                }
                
                var fields = this.GetFieldsForComparison( enhancedType, ignoredFields, config.IgnoreBaseClassFields );
                
                foreach ( var field in fields )
                {
                    this.EmitEqualsField( writer, methodBody, field );
                }
                
                // return true;
                writer.EmitInstruction( OpCodeNumber.Ldc_I4_1 );
                writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, methodBody.ReturnVariable );
                writer.EmitBranchingInstruction( OpCodeNumber.Br, methodBody.ReturnSequence );
                writer.DetachInstructionSequence();
            }

            return equalsDeclaration;
        }

        private void InjectEqualsObject( TypeDefDeclaration enhancedType, StructuralEqualityAttribute config, MethodDefDeclaration typedEqualsMethod )
        {
            // public virtual bool Equals( Base other )
            var equalsDeclaration = new MethodDefDeclaration
            {
                Name = "Equals",
                Attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                CallingConvention = CallingConvention.HasThis
            };
            enhancedType.Methods.Add( equalsDeclaration );
            equalsDeclaration.Parameters.Add( new ParameterDeclaration( 0, "other", this.objectType ));
            equalsDeclaration.ReturnParameter = ParameterDeclaration.CreateReturnParameter( this.booleanType );
            
            using ( var writer = InstructionWriter.GetInstance() )
            {
                var methodBody = MethodBodyCreator.CreateModifiableMethodBody( writer, equalsDeclaration );
                // var typedOther = methodBody.PrincipalBlock.DefineLocalVariable( enhancedType, "typedOther" );
                
                writer.AttachInstructionSequence( methodBody.PrincipalBlock.AddInstructionSequence() );

                this.InjectReferenceEquals( writer, methodBody, enhancedType );

                switch ( config.TypeCheck )
                {
                    case TypeCheck.ExactlyTheSameTypeAsThis:
                        this.InjectExactlyTheSameTypeAsThis( writer, enhancedType );
                        break;
                    case TypeCheck.ExactlyOfType:
                        throw new NotImplementedException();
                    case TypeCheck.SameTypeOrSubtype:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                // Types are different, return false.
                writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
                
                // Go to typed check.
                writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                if ( enhancedType.IsValueType() )
                {
                    writer.EmitInstructionType( OpCodeNumber.Unbox_Any, enhancedType );
                }
                else
                {
                    writer.EmitInstructionType( OpCodeNumber.Castclass, enhancedType );
                }
                
                writer.EmitInstructionMethod( OpCodeNumber.Call, typedEqualsMethod );
                
                writer.EmitBranchingInstruction( OpCodeNumber.Br_S, methodBody.ReturnSequence );
                writer.DetachInstructionSequence();
            }
        }

        private void InjectExactlyTheSameTypeAsThis( InstructionWriter writer, TypeDefDeclaration enhancedType )
        {
            writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
            if ( enhancedType.IsValueType() )
            {
                var canonicalType = enhancedType.GetCanonicalGenericInstance();
                writer.EmitInstructionType( OpCodeNumber.Ldobj, canonicalType );
                writer.EmitInstructionType( OpCodeNumber.Box, canonicalType );
            }
            
            writer.EmitInstructionMethod( OpCodeNumber.Call, this.getTypeMethod );
            
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionMethod( OpCodeNumber.Callvirt, this.getTypeMethod );
            
            writer.EmitInstruction( OpCodeNumber.Ceq );
        }

        private void EmitEqualsField( InstructionWriter writer, CreatedEmptyMethod methodBody,
            FieldDefDeclaration field )
        {
            ITypeSignature fieldType = field.FieldType;
            // TODO: Collections.
            // var isCollection = fieldType.IsCollection();
            
            if ( (fieldType.GetNakedType() is IntrinsicTypeSignature || fieldType.IsEnum() ) )
            {
                this.EmitSimpleValueCheck( writer, methodBody, field );
            }
            // else if (!isCollection || fieldType.IsGenericDefinition)
            else
            {
                this.EmitNormalCheck( writer, methodBody, field );
            }
            // else
            // {
            //     this.EmitCollectionCheck( writer, methodBody, field, collectionEqualsMethod );
            // }
        }

        private void InjectReferenceEquals( InstructionWriter writer, CreatedEmptyMethod methodBody, TypeDefDeclaration enhancedType )
        {
            if ( enhancedType.IsValueType() )
            {
                return;
            }
            
            var referenceEqualsOtherSequence = methodBody.PrincipalBlock.AddInstructionSequence();
            var structureCheckSequence = methodBody.PrincipalBlock.AddInstructionSequence();
            
            // if ( ReferenceEquals( null, other ) ) return false;
            writer.EmitInstruction( OpCodeNumber.Ldnull );
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionMethod(OpCodeNumber.Call, this.referenceEqualsMethod );
            writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, referenceEqualsOtherSequence );
            
            // return false;
            writer.EmitBranchingInstruction( OpCodeNumber.Br, methodBody.ReturnSequence );
            writer.DetachInstructionSequence();
            
            // if ( ReferenceEquals( this, other ) ) return true;
            writer.AttachInstructionSequence( referenceEqualsOtherSequence );
            
            writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionMethod(OpCodeNumber.Call, this.referenceEqualsMethod );
            writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, structureCheckSequence );
            
            // return true;
            writer.EmitInstruction( OpCodeNumber.Ldc_I4_1 );
            writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, methodBody.ReturnVariable );
            writer.EmitBranchingInstruction( OpCodeNumber.Br, methodBody.ReturnSequence );
            writer.DetachInstructionSequence();
            
            writer.AttachInstructionSequence( structureCheckSequence );
        }

        private void EmitNormalCheck( InstructionWriter writer, CreatedEmptyMethod methodBody, FieldDefDeclaration field )
        {
            var canonicalType = field.GetCanonicalGenericInstance().FieldType;
            
            void EmitLoadArg( OpCodeNumber ldarg )
            {
                writer.EmitInstruction( ldarg );
                writer.EmitInstructionField( OpCodeNumber.Ldfld, field );
                // TODO: FieldType is a value type or a generic parameter.
                if ( field.FieldType is GenericParameterTypeSignature || field.FieldType.IsValueType() )
                {
                    writer.EmitInstructionType( OpCodeNumber.Box, canonicalType );
                }
            }
            
            EmitLoadArg( OpCodeNumber.Ldarg_0 );
            EmitLoadArg( OpCodeNumber.Ldarg_1 );
            
            writer.EmitInstructionMethod( OpCodeNumber.Call, this.staticEqualsMethod );
            writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
        }

        private void EmitSimpleValueCheck( InstructionWriter writer, CreatedEmptyMethod methodBody, FieldDefDeclaration field )
        {
            writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
            writer.EmitInstructionField( OpCodeNumber.Ldfld, field );
            
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionField( OpCodeNumber.Ldfld, field );
            
            writer.EmitInstruction( OpCodeNumber.Ceq );
            writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
        }

        private void EmitCollectionCheck( InstructionWriter writer, CreatedEmptyMethod methodBody,
            FieldDefDeclaration field, IMethod collectionEqualsMethod )
        {
            InstructionSequence collectionEqualsSequence = methodBody.PrincipalBlock.AddInstructionSequence();
            
            if ( field.FieldType.IsValueType() )
            {
                this.EmitCollectionEqualsCall( writer, methodBody, field, collectionEqualsMethod );
                return;
            }

            // Check for null == null.
            writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
            writer.EmitInstruction( OpCodeNumber.Ldnull );
            writer.EmitInstruction( OpCodeNumber.Ceq );
            writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, collectionEqualsSequence );
            
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstruction( OpCodeNumber.Ldnull );
            writer.EmitInstruction( OpCodeNumber.Ceq );
            writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
            
            writer.EmitInstruction( OpCodeNumber.Ldc_I4_1 );
            writer.EmitBranchingInstruction( OpCodeNumber.Br, methodBody.ReturnSequence );
            writer.DetachInstructionSequence();
            
            writer.AttachInstructionSequence( collectionEqualsSequence );
            this.EmitCollectionEqualsCall( writer, methodBody, field, collectionEqualsMethod );
        }

        private void EmitCollectionEqualsCall( InstructionWriter writer, CreatedEmptyMethod methodBody,
            FieldDefDeclaration field, IMethod collectionEqualsMethod )
        {
            void EmitLoadArgument( OpCodeNumber ldarg )
            {
                writer.EmitInstruction( ldarg );
                // TODO: Array.
                if ( field.FieldType.IsValueType() )
                {
                    writer.EmitInstructionType( OpCodeNumber.Box, field.FieldType );
                }
            }

            EmitLoadArgument( OpCodeNumber.Ldarg_0 );
            EmitLoadArgument( OpCodeNumber.Ldarg_1 );
            
            writer.EmitInstructionMethod( OpCodeNumber.Call, collectionEqualsMethod );
        }

        private IEnumerable<FieldDefDeclaration> GetFieldsForComparison( TypeDefDeclaration enhancedType,
            ISet<FieldDefDeclaration> ignoredFields, bool ignoreBaseClassFields )
        {
            foreach ( FieldDefDeclaration field in enhancedType.Fields )
            {
                if ( field.IsConst || field.IsStatic || ignoredFields.Contains( field ) )
                {
                    continue;
                }
                
                // TODO ignoreBaseClassFields.

                yield return field;
            }
        }
    }
}