using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Community.StructuralEquality.Weaver.Subroutines;
using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Helpers;
using PostSharp.Sdk.Extensibility;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    public class HashCodeInjection
    {
        private readonly int magicNumber = 397;
        private readonly INamedType IEnumeratorType;
        private readonly IMethod Object_GetHashCode;
        private readonly IMethod MoveNext;
        private readonly IMethod GetCurrent;
        private readonly IMethod GetEnumerator;
        private readonly Project project;

        public HashCodeInjection(Project project)
        {
            this.project = project;

            // Find Object.GetHashCode():
            ModuleDeclaration module = this.project.Module;
            INamedType tObject = module.Cache.GetIntrinsic(IntrinsicType.Object).GetTypeDefinition();
            Object_GetHashCode = module.FindMethod(tObject, "GetHashCode");

            // Find IEnumerator.MoveNext() and IEnumerator.Current:
            IEnumeratorType = (INamedType) module.Cache.GetType(typeof(IEnumerator));
            MoveNext = module.FindMethod(IEnumeratorType, "MoveNext");
            GetCurrent = module.FindMethod(IEnumeratorType, "get_Current");

            // Find IEnumerable.GetEnumerator()
            INamedType tEnumerable = (INamedType) module.Cache.GetType(typeof(IEnumerable));
            GetEnumerator = module.FindMethod(tEnumerable, "GetEnumerator");
        }

        public void AddGetHashCodeTo(TypeDefDeclaration enhancedType, StructuralEqualityAttribute config,
            ISet<FieldDefDeclaration> ignoredFields)
        {
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
            enhancedType.Methods.Add(method);
            CompilerGeneratedAttributeHelper.AddCompilerGeneratedAttribute(method);
            method.ReturnParameter =
                ParameterDeclaration.CreateReturnParameter(enhancedType.Module.Cache.GetIntrinsic(IntrinsicType.Int32));

            // Generate ReSharper-style Fowler–Noll–Vo hash:
            using (InstructionWriter writer = InstructionWriter.GetInstance())
            {
                CreatedEmptyMethod getHashCodeData = MethodBodyCreator.CreateModifiableMethodBody(writer, method);
                var resultVariable = getHashCodeData.ReturnVariable;
                writer.AttachInstructionSequence(getHashCodeData.PrincipalBlock.AddInstructionSequence());

                // Start with 0
                writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, resultVariable);
                bool first = true;

                // Add base.GetHashCode():
                if (!config.IgnoreBaseClass)
                {
                    bool ignorable = enhancedType.BaseTypeDef.Name == "System.Object" ||
                                     enhancedType.IsValueTypeSafe() == true;
                    if (!ignorable)
                    {
                        var baseHashCode = project.Module.FindMethod(enhancedType.BaseTypeDef, "GetHashCode",
                            BindingOptions.DontThrowException, 0);
                        // TODO Gael says: using FindOverride would be better
                        if (baseHashCode != null)
                        {
                            writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, resultVariable);
                            writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                            // TODO what if it is two steps removed? then we won't call it!
                            writer.EmitInstructionMethod(OpCodeNumber.Call,
                                baseHashCode.GetGenericInstance(enhancedType.BaseType.GetGenericContext()));
                            writer.EmitInstruction(OpCodeNumber.Add);
                            writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, resultVariable);
                            first = false;
                        }
                    }
                }

                // For each field, do "hash = hash * 397 ^ field?.GetHashCode();
                foreach (FieldDefDeclaration field in enhancedType.Fields)
                {
                    if (field.IsConst || field.IsStatic || ignoredFields.Contains(field))
                    {
                        continue;
                    }

                    this.AddFieldCode(field, first, writer, resultVariable, method, enhancedType);
                    first = false;
                }

                // Now custom logic:
                foreach (var customLogic in enhancedType.Methods)
                {
                    if (customLogic.CustomAttributes.GetOneByType(typeof(AdditionalGetHashCodeMethodAttribute)
                        .FullName) != null)
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
                writer.EmitBranchingInstruction(OpCodeNumber.Br, getHashCodeData.ReturnSequence);
                writer.DetachInstructionSequence();
            }
        }

        private void AddCustomLogicCall(TypeDefDeclaration enhancedType, InstructionWriter writer,
            MethodDefDeclaration customMethod)
        {
            var parameters = customMethod.Parameters;
            if (parameters.Count != 0)
            {
                Message.Write(enhancedType, SeverityType.Error, "EQU1", "The signature of a method annotated with ["
                                                                        + nameof(AdditionalGetHashCodeMethodAttribute) +
                                                                        "] must be 'int MethodName()'. It can't have parameters.");
                writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
                return;
            }

            if (!customMethod.ReturnParameter.ParameterType.IsIntrinsic(IntrinsicType.Int32))
            {
                Message.Write(enhancedType, SeverityType.Error, "EQU2", "The signature of a method annotated with ["
                                                                        + nameof(AdditionalGetHashCodeMethodAttribute) +
                                                                        "] must be 'int MethodName()'. Its return type must be 'int'.");
                writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
                return;
            }

            writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            writer.EmitInstructionMethod(
                enhancedType.IsValueTypeSafe() == true ? OpCodeNumber.Call : OpCodeNumber.Callvirt,
                customMethod.GetCanonicalGenericInstance());
        }

        private void AddFieldCode(FieldDefDeclaration field, bool isFirst, InstructionWriter writer,
            LocalVariableSymbol resultVariable, MethodDefDeclaration method, TypeDefDeclaration enhancedType)
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
                AddNullableProperty(field, writer);
            }
            else if (isCollection && propType.GetReflectionName() != "System.String")
            {
                AddCollectionCode(field, writer, resultVariable, method, enhancedType);
            }
            else if (isValueType || isGenericParameter)
            {
                LoadVariable(field, writer);
                if (propType.GetReflectionName() != "System.Int32")
                {
                    writer.EmitInstructionType(OpCodeNumber.Box, propType);
                    writer.EmitInstructionMethod(OpCodeNumber.Callvirt, Object_GetHashCode);
                }
            }
            else
            {
                LoadVariable(field, writer);
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
            if (field.FieldType.IsValueTypeSafe() == true)
            {
                AddCollectionCodeInternal(field, resultVariable, method, enhancedType, writer);
            }
            else
            {
                LoadVariable(field, writer);
                writer.IfNotZero(
                    thenw => { AddCollectionCodeInternal(field, resultVariable, method, enhancedType, thenw); },
                    elsew => { });
            }
        }

        private void AddCollectionCodeInternal(FieldDefDeclaration field, LocalVariableSymbol resultVariable,
            MethodDefDeclaration method, TypeDefDeclaration enhancedType, InstructionWriter writer)
        {
            LoadVariable(field, writer);

            var enumeratorVariable =
                method.MethodBody.RootInstructionBlock.DefineLocalVariable(IEnumeratorType, "enumeratorVariable");
            var currentVariable =
                method.MethodBody.RootInstructionBlock.DefineLocalVariable(
                    method.Module.Cache.GetIntrinsic(IntrinsicType.Object), "enumeratorObject");

            AddGetEnumerator(writer, enumeratorVariable, field);

            AddCollectionLoop(resultVariable, writer, enumeratorVariable, currentVariable);
        }

        private void AddCollectionLoop(LocalVariableSymbol resultVariable, InstructionWriter t,
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
                            bt.EmitInstructionMethod(OpCodeNumber.Callvirt, Object_GetHashCode);
                        },
                        et => { et.EmitInstruction(OpCodeNumber.Ldc_I4_0); });

                    b.EmitInstruction(OpCodeNumber.Xor);
                    b.EmitInstructionLocalVariable(OpCodeNumber.Stloc, resultVariable);
                });
        }

        void AddGetEnumerator(InstructionWriter ins, LocalVariableSymbol variable, FieldDefDeclaration field)
        {
            if (field.FieldType.IsValueTypeSafe() == true)
            {
                ins.EmitInstructionType(OpCodeNumber.Box, field.FieldType);
            }

            ins.EmitInstructionMethod(OpCodeNumber.Callvirt, GetEnumerator);
            ins.EmitInstructionLocalVariable(OpCodeNumber.Stloc, variable);
        }

        private void AddNullableProperty(FieldDefDeclaration field, InstructionWriter writer)
        {
            IMethodSignature getHasValue = new MethodSignature(field.Module, CallingConvention.HasThis,
                field.Module.Cache.GetIntrinsic(IntrinsicType.Boolean), new List<ITypeSignature>(), 0);
            var hasValueMethod = field.FieldType.FindMethod("get_HasValue", getHasValue);
            writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            writer.EmitInstructionField(OpCodeNumber.Ldflda, field.GetCanonicalGenericInstance());
            writer.EmitInstructionMethod(OpCodeNumber.Call,
                hasValueMethod.GetInstance(field.Module, hasValueMethod.GenericMap));

            writer.IfNotZero((then) =>
                {
                    writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    writer.EmitInstructionField(OpCodeNumber.Ldfld, field.GetCanonicalGenericInstance());
                    writer.EmitInstructionType(OpCodeNumber.Box, field.FieldType);
                    writer.EmitInstructionMethod(OpCodeNumber.Callvirt, Object_GetHashCode);
                },
                (elseBranch) => { writer.EmitInstruction(OpCodeNumber.Ldc_I4_0); });
        }

        private void AddNormalCode(FieldDefDeclaration field, InstructionWriter writer, TypeDefDeclaration enhancedType)
        {
            writer.IfNotZero(
                thenw =>
                {
                    LoadVariable(field, thenw);
                    thenw.EmitInstructionMethod(OpCodeNumber.Callvirt, Object_GetHashCode);
                },
                elsew => { elsew.EmitInstruction(OpCodeNumber.Ldc_I4_0); });
        }

        private void LoadVariable(FieldDefDeclaration field, InstructionWriter writer)
        {
            writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            writer.EmitInstructionField(OpCodeNumber.Ldfld, field.GetCanonicalGenericInstance());
        }

        private void AddMultiplicityByMagicNumber(bool isFirst, InstructionWriter writer,
            LocalVariableSymbol resultVariable, bool isCollection)
        {
            if (!isFirst && !isCollection)
            {
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, resultVariable);
                writer.EmitInstructionInt32(OpCodeNumber.Ldc_I4, magicNumber);
                writer.EmitInstruction(OpCodeNumber.Mul);
            }
        }
    }
}