using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Community.StructuralEquality.Weaver.Subroutines;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Helpers;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.Extensibility;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    
    
    
    public class EqualsInjection
    {
        private readonly IGenericMethodDefinition referenceEqualsMethod;
        private readonly IntrinsicTypeSignature objectType;
        private readonly TypeDefDeclaration objectTypeDef;
        private readonly ITypeSignature booleanType;
        private readonly IGenericMethodDefinition getTypeMethod;
        private readonly IGenericMethodDefinition instanceEqualsMethod;
        private readonly IGenericMethodDefinition staticEqualsMethod;
        private readonly IGenericMethodDefinition collectionEqualsMethod;
        private readonly IGenericMethodDefinition getTypeFromHandleMethod;

        public EqualsInjection(Project project)
        {
            this.objectType = project.Module.Cache.GetIntrinsic( IntrinsicType.Object );
            this.booleanType = project.Module.Cache.GetIntrinsic( IntrinsicType.Boolean );
            
            this.objectTypeDef = this.objectType.GetTypeDefinition();
            
            this.referenceEqualsMethod = project.Module.FindMethod( objectTypeDef, "ReferenceEquals" );
            this.instanceEqualsMethod = project.Module.FindMethod( objectTypeDef, "Equals", 1 );
            this.staticEqualsMethod = project.Module.FindMethod( objectTypeDef, "Equals", 2 );
            this.getTypeMethod = project.Module.FindMethod( objectTypeDef, "GetType" );
            
            var collectionHelperTypeDef = project.Module.Cache.GetType( typeof(CollectionHelper) ).GetTypeDefinition();
            this.collectionEqualsMethod = project.Module.FindMethod( collectionHelperTypeDef, "Equals", declaration => declaration.IsStatic );

            var typeTypeDef = project.Module.Cache.GetType( typeof(Type) ).GetTypeDefinition();
            this.getTypeFromHandleMethod = project.Module.FindMethod( typeTypeDef, "GetTypeFromHandle" );
        }
        
        public void AddEqualsTo( TypeDefDeclaration enhancedType, StructuralEqualityAttribute config,
            ISet<FieldDefDeclaration> ignoredFields )
        {
            var typedEqualsMethod = this.InjectEqualsType(enhancedType, config, ignoredFields );
            this.InjectEqualsObject(enhancedType, config, typedEqualsMethod);
        }

        private MethodDefDeclaration InjectEqualsType( TypeDefDeclaration enhancedType,
            StructuralEqualityAttribute config, ICollection<FieldDefDeclaration> ignoredFields )
        {
            // public virtual bool Equals( Typed other )
            var equalsDeclaration = new MethodDefDeclaration
            {
                Name = "Equals",
                Attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                CallingConvention = CallingConvention.HasThis
            };
            enhancedType.Methods.Add( equalsDeclaration );
            equalsDeclaration.Parameters.Add( new ParameterDeclaration( 0, "other", enhancedType ));
            equalsDeclaration.ReturnParameter = ParameterDeclaration.CreateReturnParameter( this.booleanType );
            CompilerGeneratedAttributeHelper.AddCompilerGeneratedAttribute(equalsDeclaration);
            
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

                if ( !config.IgnoreBaseClass && !enhancedType.IsValueType() && enhancedType.BaseTypeDef != this.objectTypeDef )
                {
                    var baseEqualsMethod = this.instanceEqualsMethod.FindOverride( enhancedType.BaseTypeDef, true ).Method;
                    
                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
                    writer.EmitInstructionMethod( OpCodeNumber.Call, baseEqualsMethod.Translate( enhancedType.Module ) );
                    
                    // base.Equals(other) returned false.
                    writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
                }
                
                var fields = GetFieldsForComparison( enhancedType, ignoredFields );
                
                foreach ( var field in fields )
                {
                    this.EmitEqualsField( writer, methodBody, field );
                }
                
                InjectCustomMethods( enhancedType, writer, methodBody );

                // return true;
                writer.EmitInstruction( OpCodeNumber.Ldc_I4_1 );
                writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, methodBody.ReturnVariable );
                writer.EmitBranchingInstruction( OpCodeNumber.Br, methodBody.ReturnSequence );
                writer.DetachInstructionSequence();
            }

            return equalsDeclaration;
        }

        private static void InjectCustomMethods( TypeDefDeclaration enhancedType, InstructionWriter writer,
            CreatedEmptyMethod methodBody )
        {
            // Custom equality methods.
            foreach ( var customEqualsMethod in enhancedType.Methods )
            {
                if ( customEqualsMethod.CustomAttributes.GetOneByType(
                         "PostSharp.Community.StructuralEquality.AdditionalEqualsMethodAttribute" ) != null )
                {
                    if ( customEqualsMethod.IsStatic ||
                         !customEqualsMethod.ReturnParameter.ParameterType.IsIntrinsic( IntrinsicType.Boolean ) ||
                         customEqualsMethod.Parameters.Count != 1 ||
                         !customEqualsMethod.Parameters[0].ParameterType.GetTypeDefinition().Equals( enhancedType )
                    )
                    {
                        CustomMethodSignatureError();
                    }

                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
                    writer.EmitInstructionMethod( OpCodeNumber.Call, customEqualsMethod );
                    writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
                }
            }
        }

        private static void CustomMethodSignatureError()
        {
            throw new Exception("Method marked with [CustomEqualsInternal] must be public, instance, must return bool and accept 1 parameter of the same type as the declaring type");
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
            CompilerGeneratedAttributeHelper.AddCompilerGeneratedAttribute(equalsDeclaration);
            
            using ( var writer = InstructionWriter.GetInstance() )
            {
                var methodBody = MethodBodyCreator.CreateModifiableMethodBody( writer, equalsDeclaration );
                // var typedOther = methodBody.PrincipalBlock.DefineLocalVariable( enhancedType, "typedOther" );
                
                writer.AttachInstructionSequence( methodBody.PrincipalBlock.AddInstructionSequence() );

                this.InjectReferenceEquals( writer, methodBody, enhancedType );

                if ( enhancedType.IsValueType() )
                {
                    // if (other is Typed)
                    writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
                    writer.EmitInstructionType( OpCodeNumber.Isinst, enhancedType );
                    writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
                    
                    // return this.Equals((Typed)other);
                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
                    writer.EmitInstructionType( OpCodeNumber.Unbox_Any, enhancedType );
                    writer.EmitInstructionMethod( OpCodeNumber.Call, typedEqualsMethod );
                    
                    writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, methodBody.ReturnVariable );
                    writer.EmitBranchingInstruction( OpCodeNumber.Br, methodBody.ReturnSequence );
                    
                    writer.DetachInstructionSequence();
                    return;
                }
                
                // Reference types.
                
                switch ( config.TypeCheck )
                {
                    case TypeCheck.ExactlyTheSameTypeAsThis:
                        this.InjectExactlyTheSameTypeAsThis( writer, enhancedType );
                        break;
                    case TypeCheck.ExactlyOfType:
                        this.InjectExactlyOfType( writer, enhancedType );
                        break;
                    case TypeCheck.SameTypeOrSubtype:
                        this.InjectSameTypeOrSubtype( writer, enhancedType );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                // Types are different, return false.
                writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
                
                // Go to typed check.
                writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
                writer.EmitInstructionType( OpCodeNumber.Castclass, enhancedType );
                
                writer.EmitInstructionMethod( OpCodeNumber.Call, typedEqualsMethod );
                
                writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, methodBody.ReturnVariable );
                writer.EmitBranchingInstruction( OpCodeNumber.Br, methodBody.ReturnSequence );
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

        private void InjectExactlyOfType( InstructionWriter writer, TypeDefDeclaration enhancedType )
        {
            writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
            if ( enhancedType.IsValueType() )
            {
                var canonical = enhancedType.GetCanonicalGenericInstance();
                writer.EmitInstructionType( OpCodeNumber.Ldobj, canonical );
                writer.EmitInstructionType( OpCodeNumber.Box, canonical );
            }
            
            writer.EmitInstructionMethod( OpCodeNumber.Call, this.getTypeMethod );
            
            writer.EmitInstructionType( OpCodeNumber.Ldtoken, enhancedType );
            writer.EmitInstructionMethod( OpCodeNumber.Call, this.getTypeFromHandleMethod );
            
            writer.EmitInstruction( OpCodeNumber.Ceq );
        }

        private void InjectSameTypeOrSubtype( InstructionWriter writer, TypeDefDeclaration enhancedType )
        {
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionType( OpCodeNumber.Isinst, enhancedType );
        }

        private void EmitEqualsField( InstructionWriter writer, CreatedEmptyMethod methodBody,
            FieldDefDeclaration field )
        {
            ITypeSignature fieldType = field.FieldType;
            var isCollection = fieldType.IsCollection() || fieldType is ArrayTypeSignature;
            
            if ( (fieldType.GetNakedType() is IntrinsicTypeSignature || fieldType.IsEnum() ) )
            {
                this.EmitSimpleValueCheck( writer, methodBody, field );
            }
            // TODO: generics?
            else if (isCollection)
            {
                this.EmitCollectionCheck( writer, methodBody, field );
            }
            else
            {
                this.EmitNormalCheck( writer, methodBody, field );
            }
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

        private void EmitCollectionCheck( InstructionWriter writer, CreatedEmptyMethod methodBody, IField field )
        {
            void EmitLoadArgument( OpCodeNumber ldarg )
            {
                writer.EmitInstruction( ldarg );
                writer.EmitInstructionField( OpCodeNumber.Ldfld, field );
                // TODO: boxing.
                // if ( field.FieldType.IsValueType() && !(field.FieldType is ArrayTypeSignature) )
                // {
                //     writer.EmitInstructionType( OpCodeNumber.Box, field.FieldType );
                // }
            }
            
            EmitLoadArgument( OpCodeNumber.Ldarg_0 );
            EmitLoadArgument( OpCodeNumber.Ldarg_1 );
            
            writer.EmitInstructionMethod( OpCodeNumber.Call, this.collectionEqualsMethod );
            
            // Collections don't match, return false.
            writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
        }

        private static IEnumerable<FieldDefDeclaration> GetFieldsForComparison( TypeDefDeclaration enhancedType,
            ICollection<FieldDefDeclaration> ignoredFields )
        {
            foreach ( FieldDefDeclaration field in enhancedType.Fields )
            {
                if ( field.IsConst || field.IsStatic || ignoredFields.Contains( field ) )
                {
                    continue;
                }

                yield return field;
            }
        }
    }
}