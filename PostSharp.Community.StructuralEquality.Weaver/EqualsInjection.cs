using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Community.StructuralEquality.Weaver.Subroutines;
using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Helpers;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.Extensibility;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    /// <summary>
    /// Adds IEquatable&lt;T&gt; and implements equality methods.
    /// </summary>
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
        private readonly ITypeSignature equatableInterface;

        public EqualsInjection( Project project )
        {
            this.objectType = project.Module.Cache.GetIntrinsic( IntrinsicType.Object );
            this.booleanType = project.Module.Cache.GetIntrinsic( IntrinsicType.Boolean );

            this.objectTypeDef = this.objectType.GetTypeDefinition();

            this.referenceEqualsMethod = project.Module.FindMethod( objectTypeDef, "ReferenceEquals" );
            this.instanceEqualsMethod = project.Module.FindMethod( objectTypeDef, "Equals", 1 );
            this.staticEqualsMethod = project.Module.FindMethod( objectTypeDef, "Equals", 2 );
            this.getTypeMethod = project.Module.FindMethod( objectTypeDef, "GetType" );

            var collectionHelperTypeDef = project.Module.Cache.GetType( typeof(CollectionHelper) ).GetTypeDefinition();
            this.collectionEqualsMethod = project.Module.FindMethod( collectionHelperTypeDef, "Equals",
                declaration => declaration.IsStatic );

            var typeTypeDef = project.Module.Cache.GetType( typeof(Type) ).GetTypeDefinition();
            this.getTypeFromHandleMethod = project.Module.FindMethod( typeTypeDef, "GetTypeFromHandle" );

            this.equatableInterface = project.Module.Cache.GetType( typeof(IEquatable<>) );
        }

        public void AddEqualsTo( TypeDefDeclaration enhancedType, StructuralEqualityAttribute config,
            ISet<FieldDefDeclaration> ignoredFields )
        {
            var typedEqualsMethod = this.InjectEqualsType( enhancedType, config, ignoredFields );
            this.InjectEqualsObject( enhancedType, config, typedEqualsMethod );

            this.AddEquatable( enhancedType );
        }

        private void AddEquatable( TypeDefDeclaration enhancedType )
        {
            var genericInstance = this.equatableInterface.GetTypeDefinition().GetGenericInstance( new GenericMap(
                enhancedType.Module,
                new[] {enhancedType.GetCanonicalGenericInstance()} ) );
            ITypeSignature interfaceRef = genericInstance.TranslateType( enhancedType.Module );

            enhancedType.InterfaceImplementations.Add( interfaceRef );
        }

        private MethodDefDeclaration InjectEqualsType( TypeDefDeclaration enhancedType,
            StructuralEqualityAttribute config, ICollection<FieldDefDeclaration> ignoredFields )
        {
            IType genericTypeInstance = enhancedType.GetCanonicalGenericInstance();

            var existingMethod = enhancedType.Methods.FirstOrDefault<IMethod>( declaration =>
            {
                return declaration.IsPublic() &&
                       !declaration.IsStatic &&
                       declaration.ParameterCount == 1 &&
                       declaration.GetParameterType( 0 ).Equals( genericTypeInstance );
            } );

            if ( existingMethod != null )
            {
                return existingMethod.GetMethodDefinition();
            }
            
            // public virtual bool Equals( Typed other )
            var equalsDeclaration = new MethodDefDeclaration
            {
                Name = "Equals",
                Attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                CallingConvention = CallingConvention.HasThis
            };
            enhancedType.Methods.Add( equalsDeclaration );
            equalsDeclaration.Parameters.Add( new ParameterDeclaration( 0, "other", genericTypeInstance ) );
            equalsDeclaration.ReturnParameter = ParameterDeclaration.CreateReturnParameter( this.booleanType );
            CompilerGeneratedAttributeHelper.AddCompilerGeneratedAttribute( equalsDeclaration );

            using ( var writer = InstructionWriter.GetInstance() )
            {
                var methodBody = MethodBodyCreator.CreateModifiableMethodBody( writer, equalsDeclaration );
                writer.AttachInstructionSequence( methodBody.PrincipalBlock.AddInstructionSequence() );
                
                writer.EmitInstruction( OpCodeNumber.Ldc_I4_0 );
                writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, methodBody.ReturnVariable );

                this.InjectReferenceEquals( writer, methodBody, enhancedType );
                // Writer is either attached to the same sequence (value types) or to a new one which should check the structure.

                // return base.Equals(other) && this.field1 == other.field1 && ...;
                if ( !config.IgnoreBaseClass && enhancedType.IsValueTypeSafe() != true )
                {
                    // Find the base method.
                    var baseEqualsMethod = this.instanceEqualsMethod.FindOverride( enhancedType.BaseTypeDef, true )
                        .GetInstance( enhancedType.Module, enhancedType.BaseType.GetGenericContext() );

                    // Do not invoke object.Equals();
                    if ( baseEqualsMethod.DeclaringType.GetTypeDefinition() != this.objectTypeDef )
                    {
                        writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                        writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
                        writer.EmitInstructionMethod( OpCodeNumber.Call, baseEqualsMethod );

                        // base.Equals(other) returned false, go to return.
                        writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
                    }
                }

                foreach ( var field in GetFieldsForComparison( enhancedType, ignoredFields ) )
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
                        CustomMethodSignatureError(enhancedType, customEqualsMethod);
                    }

                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
                    writer.EmitInstructionMethod( OpCodeNumber.Call, customEqualsMethod.GetCanonicalGenericInstance() );
                    writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
                }
            }
        }

        private static void CustomMethodSignatureError(TypeDefDeclaration enhancedType, MethodDefDeclaration method)
        {
            string message =
                $"Method {method.Name} marked with [CustomEqualsInternal] must be public, instance, must return bool and accept 1 parameter of the same type as the declaring type {enhancedType.Name}";
            throw new InjectionException( "EQU3", message );
        }

        private void InjectEqualsObject( TypeDefDeclaration enhancedType, StructuralEqualityAttribute config,
            MethodDefDeclaration typedEqualsMethod )
        {
            var existingMethod = enhancedType.Methods.FirstOrDefault<IMethod>( declaration =>
            {
                return declaration.Name == "Equals" &&
                       declaration.IsPublic() &&
                       !declaration.IsStatic &&
                       declaration.ParameterCount == 1 &&
                       declaration.GetParameterType( 0 ).Equals( this.objectType );
            } );

            if ( existingMethod != null )
            {
                return;
            }
            
            // public virtual bool Equals( object other )
            var equalsDeclaration = new MethodDefDeclaration
            {
                Name = "Equals",
                Attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                CallingConvention = CallingConvention.HasThis
            };
            enhancedType.Methods.Add( equalsDeclaration );
            equalsDeclaration.Parameters.Add( new ParameterDeclaration( 0, "other", this.objectType ) );
            equalsDeclaration.ReturnParameter = ParameterDeclaration.CreateReturnParameter( this.booleanType );
            CompilerGeneratedAttributeHelper.AddCompilerGeneratedAttribute( equalsDeclaration );

            using ( var writer = InstructionWriter.GetInstance() )
            {
                var methodBody = MethodBodyCreator.CreateModifiableMethodBody( writer, equalsDeclaration );

                writer.AttachInstructionSequence( methodBody.PrincipalBlock.AddInstructionSequence() );
                
                writer.EmitInstruction( OpCodeNumber.Ldc_I4_0 );
                writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, methodBody.ReturnVariable );

                this.InjectReferenceEquals( writer, methodBody, enhancedType );

                var genericTypeInstance = enhancedType.GetCanonicalGenericInstance();

                this.EmitTypeCheck( enhancedType, config, writer, genericTypeInstance, methodBody );
                if ( enhancedType.IsValueTypeSafe() == true )
                {
                    this.EmitEqualsObjectOfValueType( writer, genericTypeInstance, typedEqualsMethod, methodBody );
                }
                else
                {
                    this.EmitEqualsObjectOfReferenceType( writer, genericTypeInstance, typedEqualsMethod, methodBody );
                }
                
                writer.DetachInstructionSequence();
            }
        }

        private void EmitTypeCheck( TypeDefDeclaration enhancedType, StructuralEqualityAttribute config,
            InstructionWriter writer, IType genericTypeInstance, CreatedEmptyMethod methodBody )
        {
            switch ( config.TypeCheck )
            {
                case TypeCheck.ExactlyTheSameTypeAsThis:
                    this.InjectExactlyTheSameTypeAsThis( writer, enhancedType, genericTypeInstance );
                    break;
                case TypeCheck.ExactlyOfType:
                    this.InjectExactlyOfType( writer, genericTypeInstance );
                    break;
                case TypeCheck.SameTypeOrSubtype:
                    this.InjectSameTypeOrSubtype( writer, genericTypeInstance );
                    break;
                default:
                    throw new InjectionException( "EQU4", $"Unknown TypeCheck value: {config.TypeCheck}" );
            }

            // Types are different, return false.
            writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
        }

        private void EmitEqualsObjectOfReferenceType( InstructionWriter writer, IType genericTypeInstance,
            MethodDefDeclaration typedEqualsMethod, CreatedEmptyMethod methodBody )
        {
            // Go to typed check.
            writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionType( OpCodeNumber.Castclass, genericTypeInstance );
            writer.EmitInstructionMethod( OpCodeNumber.Call, typedEqualsMethod.GetCanonicalGenericInstance() );

            writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, methodBody.ReturnVariable );
            writer.EmitBranchingInstruction( OpCodeNumber.Br, methodBody.ReturnSequence );
        }

        private void EmitEqualsObjectOfValueType( InstructionWriter writer,
            IType genericTypeInstance, MethodDefDeclaration typedEqualsMethod, CreatedEmptyMethod methodBody )
        {
            // return this.Equals((Typed)other);
            writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionType( OpCodeNumber.Unbox_Any, genericTypeInstance );
            writer.EmitInstructionMethod( OpCodeNumber.Call, typedEqualsMethod.GetCanonicalGenericInstance() );

            writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, methodBody.ReturnVariable );
            writer.EmitBranchingInstruction( OpCodeNumber.Br, methodBody.ReturnSequence );
        }

        private void InjectExactlyTheSameTypeAsThis( InstructionWriter writer, ITypeSignature enhancedType, ITypeSignature genericTypeInstance )
        {
            writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
            if ( enhancedType.IsValueTypeSafe() == true )
            {
                writer.EmitInstructionType( OpCodeNumber.Ldobj, genericTypeInstance );
                writer.EmitInstructionType( OpCodeNumber.Box, genericTypeInstance );
            }

            writer.EmitInstructionMethod( OpCodeNumber.Call, this.getTypeMethod );

            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionMethod( OpCodeNumber.Callvirt, this.getTypeMethod );

            writer.EmitInstruction( OpCodeNumber.Ceq );
        }

        private void InjectExactlyOfType( InstructionWriter writer, ITypeSignature genericTypeInstance )
        {
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionMethod( OpCodeNumber.Call, this.getTypeMethod );

            writer.EmitInstructionType( OpCodeNumber.Ldtoken, genericTypeInstance );
            writer.EmitInstructionMethod( OpCodeNumber.Call, this.getTypeFromHandleMethod );

            writer.EmitInstruction( OpCodeNumber.Ceq );
        }

        private void InjectSameTypeOrSubtype( InstructionWriter writer, ITypeSignature genericTypeInstance )
        {
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionType( OpCodeNumber.Isinst, genericTypeInstance );
        }

        private void EmitEqualsField( InstructionWriter writer, CreatedEmptyMethod methodBody,
            FieldDefDeclaration field )
        {
            ITypeSignature fieldType = field.FieldType;

            if ( ( fieldType.GetNakedType() is IntrinsicTypeSignature || fieldType.IsEnum() ) )
            {
                this.EmitSimpleValueCheck( writer, methodBody, field );
            }
            else if ( fieldType.IsCollection() || fieldType is ArrayTypeSignature )
            {
                this.EmitCollectionCheck( writer, methodBody, field );
            }
            else
            {
                this.EmitNormalCheck( writer, methodBody, field );
            }
        }

        private void InjectReferenceEquals( InstructionWriter writer, CreatedEmptyMethod methodBody,
            TypeDefDeclaration enhancedType )
        {
            if ( enhancedType.IsValueTypeSafe() == true )
            {
                return;
            }

            var referenceEqualsOtherSequence = methodBody.PrincipalBlock.AddInstructionSequence();
            var structureCheckSequence = methodBody.PrincipalBlock.AddInstructionSequence();

            // if ( ReferenceEquals( null, other ) ) return false;
            writer.EmitInstruction( OpCodeNumber.Ldnull );
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionMethod( OpCodeNumber.Call, this.referenceEqualsMethod );
            writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, referenceEqualsOtherSequence );

            // return false;
            writer.EmitBranchingInstruction( OpCodeNumber.Br, methodBody.ReturnSequence );
            writer.DetachInstructionSequence();

            // if ( ReferenceEquals( this, other ) ) return true;
            writer.AttachInstructionSequence( referenceEqualsOtherSequence );

            writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
            writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
            writer.EmitInstructionMethod( OpCodeNumber.Call, this.referenceEqualsMethod );
            writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, structureCheckSequence );

            // return true;
            writer.EmitInstruction( OpCodeNumber.Ldc_I4_1 );
            writer.EmitInstructionLocalVariable( OpCodeNumber.Stloc, methodBody.ReturnVariable );
            writer.EmitBranchingInstruction( OpCodeNumber.Br, methodBody.ReturnSequence );
            writer.DetachInstructionSequence();

            writer.AttachInstructionSequence( structureCheckSequence );
        }

        private void EmitNormalCheck( InstructionWriter writer, CreatedEmptyMethod methodBody,
            FieldDefDeclaration field )
        {
            void EmitLoadArgument( OpCodeNumber argument )
            {
                writer.EmitInstruction( argument );
                IField genericInstance = field.GetCanonicalGenericInstance();
                writer.EmitInstructionField( OpCodeNumber.Ldfld, genericInstance );
                if ( field.FieldType is GenericParameterTypeSignature || field.FieldType.IsValueTypeSafe() == true )
                {
                    writer.EmitInstructionType( OpCodeNumber.Box, genericInstance.FieldType );
                }
            }

            EmitLoadArgument( OpCodeNumber.Ldarg_0 );
            EmitLoadArgument( OpCodeNumber.Ldarg_1 );

            writer.EmitInstructionMethod( OpCodeNumber.Call, this.staticEqualsMethod );
            writer.EmitBranchingInstruction( OpCodeNumber.Brfalse, methodBody.ReturnSequence );
        }

        private void EmitSimpleValueCheck( InstructionWriter writer, CreatedEmptyMethod methodBody,
            FieldDefDeclaration field )
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
            void EmitLoadArgument( OpCodeNumber argument )
            {
                writer.EmitInstruction( argument );
                writer.EmitInstructionField( OpCodeNumber.Ldfld, field );
                if ( field.FieldType.IsValueTypeSafe() == true )
                {
                    writer.EmitInstructionType( OpCodeNumber.Box, field.FieldType );
                }
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

    internal class InjectionException : Exception
    {
        public string ErrorCode { get; }

        public InjectionException( string errorCode, string message ) : base( message )
        {
            this.ErrorCode = errorCode;
        }
    }
}