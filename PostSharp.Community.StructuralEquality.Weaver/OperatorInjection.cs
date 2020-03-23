using System;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Helpers;
using PostSharp.Sdk.Extensibility;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    /// <summary>
    /// Overwrites equality operators with a call to the equality method.
    /// </summary>
    public class OperatorInjection
    {
        private readonly IGenericMethodDefinition weaveMethod;
        private readonly IGenericMethodDefinition staticEqualsMethod;

        public OperatorInjection( Project project )
        {
            var objectTypeDef = project.Module.Cache.GetIntrinsic( IntrinsicType.Object ).GetTypeDefinition();
            this.staticEqualsMethod = project.Module.FindMethod( objectTypeDef, "Equals", 2 );
            
            var operatorTypeDef = project.Module.Cache.GetType( typeof(Operator) ).GetTypeDefinition();
            this.weaveMethod = project.Module.FindMethod( operatorTypeDef, "Weave" );
        }
        
        private void ProcessOperator( TypeDefDeclaration enhancedType, string operatorName, string operatorSourceName, bool negate )
        {
            var operatorMethod = GetOperatorMethod( enhancedType, operatorName );

            string operatorExample = $"public static bool operator {operatorSourceName}(T left, T right) => Operator.Weave(left, right);";
            
            if ( operatorMethod == null )
            {
                throw new Exception($"The equality operator was not found on type {enhancedType.Name}, implement it like this: {operatorExample}");
            }

            var operatorMethodDef = operatorMethod.GetMethodDefinition();
            this.CheckOperator( operatorMethodDef, operatorExample );

            CompilerGeneratedAttributeHelper.AddCompilerGeneratedAttribute(operatorMethodDef);
            
            this.ReplaceOperator( enhancedType, operatorMethodDef, negate );
        }
        
        public void ProcessEqualityOperators( TypeDefDeclaration enhancedType, StructuralEqualityAttribute config )
        {
            if ( config.DoNotAddEqualityOperators )
            {
                return;
            }
            
            ProcessOperator( enhancedType, "op_Equality", "==", false );
            ProcessOperator( enhancedType, "op_Inequality", "!=", true );
        }

        private static IMethod GetOperatorMethod( TypeDefDeclaration enhancedType, string name )
        {
            return enhancedType.Methods.GetMethod( name, BindingOptions.DontThrowException,
                method => method.IsStatic && method.ParameterCount == 2 &&
                          method.ReturnType.IsIntrinsic( IntrinsicType.Boolean ) );
        }

        private void ReplaceOperator( TypeDefDeclaration enhancedType, MethodDefDeclaration equalityMethodDef,
            bool negate )
        {
            InstructionBlock originalCode = equalityMethodDef.MethodBody.RootInstructionBlock;
            originalCode.Detach();

            InstructionBlock root = equalityMethodDef.MethodBody.CreateInstructionBlock();
            equalityMethodDef.MethodBody.RootInstructionBlock = root;
            var newSequence = root.AddInstructionSequence();
            using ( var writer = InstructionWriter.GetInstance() )
            {
                writer.AttachInstructionSequence( newSequence );

                if ( enhancedType.IsValueType() )
                {
                    var canonicalType = enhancedType.GetCanonicalGenericInstance();
                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstructionType( OpCodeNumber.Box, canonicalType );
                    writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
                    writer.EmitInstructionType( OpCodeNumber.Box, canonicalType );
                }
                else
                {
                    writer.EmitInstruction( OpCodeNumber.Ldarg_0 );
                    writer.EmitInstruction( OpCodeNumber.Ldarg_1 );
                }

                writer.EmitInstructionMethod( OpCodeNumber.Call, this.staticEqualsMethod );

                if ( negate )
                {
                    writer.EmitInstruction( OpCodeNumber.Ldc_I4_0 );
                    writer.EmitInstruction( OpCodeNumber.Ceq );
                }
                
                writer.EmitInstruction( OpCodeNumber.Ret );

                writer.DetachInstructionSequence();
            }
        }

        private void CheckOperator( MethodDefDeclaration operatorMethodDef, string operatorExample )
        {
            if ( !operatorMethodDef.HasBody )
            {
                throw new Exception( $"Type {operatorMethodDef.DeclaringType.Name} has an operator without a body, implement it like this: {operatorExample}" );
            }

            using ( var reader = operatorMethodDef.MethodBody.CreateInstructionReader() )
            {
                var sequence = operatorMethodDef.MethodBody.RootInstructionBlock.FirstInstructionSequence;
                reader.EnterInstructionSequence( sequence );

                for ( int i = 0; i < 4; i++ )
                {
                    if ( !reader.ReadInstruction() )
                    {
                        throw new Exception(
                            $"Type {operatorMethodDef.DeclaringType.Name} has an operator with incorrect body, implement it like this: {operatorExample}" );
                    }

                    if ( i == 2 )
                    {
                        if ( reader.CurrentInstruction.OpCodeNumber != OpCodeNumber.Call ||
                             !reader.CurrentInstruction.MethodOperand.GetMethodDefinition()
                                 .Equals( this.weaveMethod.GetMethodDefinition() ) )
                        {
                            throw new Exception(
                                $"Type {operatorMethodDef.DeclaringType.Name} has an operator with incorrect body, implement it like this: {operatorExample}" );
                        }
                    }
                }
            }
        }

    }
}