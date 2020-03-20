
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Community.StructuralEquality.Weaver.Subroutines;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Helpers;
using PostSharp.Sdk.Extensibility;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    public class HashCodeInjection
    {
        private IGenericMethodDefinition GetHashCodeMethodReference;
        private int magicNumber = 397;
        private INamedType IEnumeratorType;
        private IMethod MoveNext;
        private IMethod GetCurrent;
        private IMethod GetEnumerator;
        public Project Project { get; }

        public HashCodeInjection(Project project)
        {
            Project = project;
            // Find Object.GetHashCode():
            ModuleDeclaration module = this.Project.Module;
            INamedType tObject = module.Cache.GetIntrinsic(IntrinsicType.Object).GetTypeDefinition();
            this.GetHashCodeMethodReference = module.FindMethod(tObject, "GetHashCode");
            IEnumeratorType = (INamedType)module.Cache.GetType(typeof(IEnumerator));
            MoveNext = module.FindMethod(IEnumeratorType, "MoveNext");
            GetCurrent = module.FindMethod(IEnumeratorType, "get_Current");
            INamedType tEnumerable = (INamedType) module.Cache.GetType(typeof(IEnumerable));
            GetEnumerator = module.FindMethod(tEnumerable, "GetEnumerator");
        }
        public void AddGetHashCodeTo(TypeDefDeclaration enhancedType, StructuralEqualityAttribute config, ISet<FieldDefDeclaration> ignoredFields)
        {
            // TODO run test coverage
            // TODO add unit test for this:
            if (enhancedType.Methods.Any<IMethod>(m => m.Name == "GetHashCode" &&
                                                       m.ParameterCount == 0))
            {
                // GetHashCode already present, just keep it.
                return;
            }
            
            // Create signature
            MethodDefDeclaration method = new MethodDefDeclaration
                                               {
                                                   Name = "GetHashCode",
                                                   CallingConvention = CallingConvention.HasThis,
                                                   Attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig
                                               };
            enhancedType.Methods.Add( method );
            CompilerGeneratedAttributeHelper.AddCompilerGeneratedAttribute(method);

            method.ReturnParameter = ParameterDeclaration.CreateReturnParameter( enhancedType.Module.Cache.GetIntrinsic( IntrinsicType.Int32 ) );
            
            // Generate ReSharper-style Fowler–Noll–Vo hash:
            using ( InstructionWriter writer = InstructionWriter.GetInstance() )
            {
                CreatedEmptyMethod getHashCodeData = MethodBodyCreator.CreateModifiableMethodBody( writer, method );
                var resultVariable = getHashCodeData.ReturnVariable;
                writer.AttachInstructionSequence( getHashCodeData.PrincipalBlock.AddInstructionSequence(  ) );
                // Start with 0
                writer.EmitInstruction( OpCodeNumber.Ldc_I4_0 );
                writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, resultVariable);
                bool first = true;

                if (!config.IgnoreBaseClass)
                {
                    bool ignorable = enhancedType.BaseTypeDef.Name == "System.Object" ||
                                     enhancedType.IsValueTypeSafe() == true;
                    if (!ignorable)
                    {
                        var baseHashCode = Project.Module.FindMethod(enhancedType.BaseTypeDef, "GetHashCode", BindingOptions.DontThrowException, 0);
                        // TODO Gael says: using FindOverride would be better
                        if (baseHashCode != null)
                        {
                            writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, resultVariable);
                            writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                            // TODO what if it is two steps removed?
                            writer.EmitInstructionMethod(OpCodeNumber.Call,
                                baseHashCode.GetGenericInstance(enhancedType.BaseType.GetGenericContext()));
                            writer.EmitInstruction(OpCodeNumber.Add);
                            writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, resultVariable);
                            first = false;
                        }
                    }
                }
                
                // For each field, do "hash = hash * 397 ^ field?.GetHashCode();
                foreach ( FieldDefDeclaration field in enhancedType.Fields )
                {
                    if ( field.IsConst || field.IsStatic || ignoredFields.Contains(field) )
                    {
                        continue;
                    }
                    
                    this.AddFieldCode(field, first, writer, resultVariable, method, enhancedType);
                    first = false;
                }
                
                // Now custom logic:
                int magicNumber = 397;
                foreach (var customLogic in enhancedType.Methods)
                {
                    if (customLogic.CustomAttributes.GetOneByType(
                        "PostSharp.Community.StructuralEquality.AdditionalGetHashCodeMethodAttribute") != null)
                    {
                        writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, resultVariable);
                        writer.EmitInstructionInt32(OpCodeNumber.Ldc_I4, magicNumber);
                        writer.EmitInstruction(OpCodeNumber.Mul);
                        AddCustomLogicCall(enhancedType, writer, customLogic);
                        writer.EmitInstruction(OpCodeNumber.Xor);
                        writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, resultVariable);
                    }
                }
                
                // Return the hash:
                writer.EmitBranchingInstruction( OpCodeNumber.Br, getHashCodeData.ReturnSequence );
                writer.DetachInstructionSequence();
            }
        }

        private void AddCustomLogicCall(TypeDefDeclaration enhancedType, InstructionWriter writer, MethodDefDeclaration customMethod)
        {
            var parameters = customMethod.Parameters;
            if (parameters.Count != 0)
            {
                throw new Exception(
                    $"Custom GetHashCode of type {enhancedType.ShortName} have to have empty parameter list.");
            }

            if (!customMethod.ReturnParameter.ParameterType.IsIntrinsic(IntrinsicType.Int32))
            {
                throw new Exception($"Custom GetHashCode of type {enhancedType.ShortName} have to return int.");
            }
            
            writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            writer.EmitInstructionMethod(enhancedType.IsValueTypeSafe() == true ? OpCodeNumber.Call : OpCodeNumber.Callvirt, customMethod.GetCanonicalGenericInstance());
        }

        private void AddFieldCode(FieldDefDeclaration field, bool isFirst, InstructionWriter writer, LocalVariableSymbol resultVariable, MethodDefDeclaration method, TypeDefDeclaration enhancedType)
        {
            bool isCollection;
            var propType = field.FieldType;
            bool isValueType = propType.IsValueTypeSafe() == true;
            bool isGenericParameter = false;
            if (propType.TypeSignatureElementKind == TypeSignatureElementKind.GenericParameter ||
                propType.TypeSignatureElementKind == TypeSignatureElementKind.GenericParameterReference)
            {
                // TODO what does this mean?
                // maybe something else also needs to be checked?
                isCollection = false;
                isGenericParameter = true;
            }
            else
            {
                isCollection = propType.IsCollection() ||
                               propType.TypeSignatureElementKind == TypeSignatureElementKind.Array;
            }

            AddMultiplicityByMagicNumber(isFirst, writer, resultVariable, isCollection);

            if (propType.GetReflectionName().StartsWith("System.Nullable"))
            {
                AddNullableProperty(field, writer, enhancedType, resultVariable);
            }
            else if (isCollection && propType.GetReflectionName() != "System.String")
            {
               AddCollectionCode(field, writer, resultVariable, method, enhancedType);
            }
            else if (isValueType || isGenericParameter)
            {
                LoadVariable(field, writer, enhancedType);
                if (propType.GetReflectionName() != "System.Int32")
                {
                    writer.EmitInstructionType(OpCodeNumber.Box, propType);
                    writer.EmitInstructionMethod(OpCodeNumber.Callvirt, GetHashCodeMethodReference);
                }
            }
            else
            {
                LoadVariable(field, writer, enhancedType);
                AddNormalCode(field, writer, enhancedType);
            }

            if (!isFirst && !isCollection)
            {
                writer.EmitInstruction(OpCodeNumber.Xor);
            }

            if (!isCollection)
            {
                writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, resultVariable);
            }
        }

        private void AddCollectionCode(FieldDefDeclaration field, InstructionWriter writer,
            LocalVariableSymbol resultVariable, MethodDefDeclaration method, TypeDefDeclaration enhancedType)
        {
            LoadVariable(field, writer, enhancedType);
            writer.IfNotZero(writer =>
            {
                LoadVariable(field, writer, enhancedType);

                var enumeratorVariable =
                    method.MethodBody.RootInstructionBlock.DefineLocalVariable(IEnumeratorType, "enumeratorVariable");
                var currentVariable =
                    method.MethodBody.RootInstructionBlock.DefineLocalVariable(
                        method.Module.Cache.GetIntrinsic(IntrinsicType.Object), "enumeratorObject");

                AddGetEnumerator(writer, enumeratorVariable, field);

                AddCollectionLoop(resultVariable, writer, enumeratorVariable, currentVariable);
            }, (elsew) => { });
        }

        void AddCollectionLoop(LocalVariableSymbol resultVariable, InstructionWriter t,
            LocalVariableSymbol enumeratorVariable, LocalVariableSymbol currentVariable)
    {
        t.WhileNotZero(
            c =>
            {
                c.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, enumeratorVariable);
                c.EmitInstructionMethod(OpCodeNumber.Callvirt, MoveNext);
            },
            b =>
            {
                b.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, resultVariable);
                b.EmitInstructionInt32(OpCodeNumber.Ldc_I4, magicNumber);
                b.EmitInstruction(OpCodeNumber.Mul);

                b.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, enumeratorVariable);
                b.EmitInstructionMethod(OpCodeNumber.Callvirt, GetCurrent);
                b.EmitInstructionLocalVariable(OpCodeNumber.Stloc, currentVariable);

                b.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, currentVariable);
                
                b.IfNotZero(
                    bt =>
                    {
                        bt.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, currentVariable);
                        bt.EmitInstructionMethod(OpCodeNumber.Callvirt, GetHashCodeMethodReference);
                    },
                    et =>
                    {
                        et.EmitInstruction(OpCodeNumber.Ldc_I4_0);
                    });

                b.EmitInstruction(OpCodeNumber.Xor);
                b.EmitInstructionLocalVariable(OpCodeNumber.Stloc, resultVariable);
            });
    }
        
    void AddGetEnumerator(InstructionWriter ins, LocalVariableSymbol variable, FieldDefDeclaration property)
    {
        if (property.FieldType.IsValueTypeSafe() == true)
        {
            ins.EmitInstructionType(OpCodeNumber.Box, property.FieldType);
        }
        ins.EmitInstructionMethod(OpCodeNumber.Callvirt, GetEnumerator);
        ins.EmitInstructionLocalVariable(OpCodeNumber.Stloc, variable);
    }

        private void AddNullableProperty(FieldDefDeclaration field, InstructionWriter writer, TypeDefDeclaration enhancedType, LocalVariableSymbol resultVariable)
        {
            // TODO make nullable work
            writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
            // var hasValueMethod = enhancedType.Module.FindMethod(field.FieldType.GetTypeDefinition(), "get_HasValue");
            // field.FieldType.ContainsGenericArguments()
            // writer.EmitInstructionField(OpCodeNumber.Ldflda, field.GetCanonicalGenericInstance());
            // writer.EmitInstructionMethod(OpCodeNumber.Call, hasValueMethod.getgene());
            // writer.IfNotZero((then) =>
            // {
            //     writer.EmitInstructionField(OpCodeNumber.Ldfld, field.GetCanonicalGenericInstance());
            //     writer.EmitInstructionType(OpCodeNumber.Box, field.FieldType);
            //     writer.EmitInstructionMethod(OpCodeNumber.Callvirt, GetHashCodeMethodReference);
            // },
            // (elseBranch) =>
            // {
            //     writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
            // });
            
        }

        private void AddNormalCode(FieldDefDeclaration field, InstructionWriter writer, TypeDefDeclaration enhancedType)
        {
             writer.IfNotZero(
                 t =>
                      {
                          LoadVariable(field, writer, enhancedType);
                          writer.EmitInstructionMethod(OpCodeNumber.Callvirt, GetHashCodeMethodReference);
                      },
                      f =>
                      {
                          f.EmitInstruction(OpCodeNumber.Ldc_I4_0);
                      });
        }

        private void LoadVariable(FieldDefDeclaration field, InstructionWriter writer, TypeDefDeclaration enhancedType)
        {
            writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            writer.EmitInstructionField(OpCodeNumber.Ldfld, field.GetCanonicalGenericInstance());
        }

        private void AddMultiplicityByMagicNumber(bool isFirst, InstructionWriter writer, LocalVariableSymbol resultVariable, bool isCollection)
        {
            if (!isFirst && !isCollection)
            {
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, resultVariable);
                writer.EmitInstructionInt32(OpCodeNumber.Ldc_I4, 397);
                writer.EmitInstruction(OpCodeNumber.Mul);
            }
        }
    }
}