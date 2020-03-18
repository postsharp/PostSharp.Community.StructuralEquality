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
        private IMethod GetHashCodeMethodReference;

        public override bool Execute()
        {
            // Find Object.GetHashCode():
            INamedType tObject = this.Project.Module.Cache.GetIntrinsic(IntrinsicType.Object).GetTypeDefinition();
            this.GetHashCodeMethodReference = this.Project.Module.FindMethod(tObject, "GetHashCode");

            var annotationRepositoryService = this.Project.GetService<IAnnotationRepositoryService>();
            var ignoredFields = IgnoredFields.GetIgnoredFields(annotationRepositoryService,
                Project.GetService<ICompilerAdapterService>());

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
                    this.AddGetHashCodeTo(enhancedType, config, ignoredFields);
                }

                // TODO implement operators
            }

            return true;
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

        private void AddGetHashCodeTo(TypeDefDeclaration enhancedType, StructuralEqualityAttribute config, ISet<FieldDefDeclaration> ignoredFields)
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
                            writer.EmitInstructionMethod(OpCodeNumber.Call, baseHashCode);
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
                // TODO
                isCollection = false;
                             //   propType.IsCollection() ||
                             //  propType.TypeSignatureElementKind == TypeSignatureElementKind.Array;
            }

            AddMultiplicityByMagicNumber(isFirst, writer, resultVariable, isCollection);

            if (propType.GetReflectionName().StartsWith("System.Nullable"))
            {
                AddNullableProperty(field, writer, enhancedType, resultVariable);
            }
            else if (isCollection && propType.GetReflectionName() != "System.String")
            {
              //  AddCollectionCode(field, writer, resultVariable, method, enhancedType);
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

                    // writer.EmitInstructionInt32( OpCodeNumber.Ldc_I4, 397 );
                    // writer.EmitInstruction( OpCodeNumber.Mul );
                    // writer.EmitInstruction( OpCodeNumber.Ldarg_0 ); // TODO what if I am a struct?
                    // writer.EmitInstructionField( OpCodeNumber.Ldfld, field );
                    // // TODO check for null
                    // // TODO GetHashCode(), actually
                    // // TODO what if the field is a struct?
                    // writer.EmitInstruction( OpCodeNumber.Xor );
        }

        private void AddNullableProperty(FieldDefDeclaration field, InstructionWriter writer, TypeDefDeclaration enhancedType, LocalVariableSymbol resultVariable)
        {
            var hasValueMethod = enhancedType.Module.FindMethod(field.FieldType.GetTypeDefinition(), "get_HasValue");
            field.FieldType.ContainsGenericArguments()
            writer.EmitInstructionField(OpCodeNumber.Ldflda, field.GetCanonicalGenericInstance());
            writer.EmitInstructionMethod(OpCodeNumber.Call, hasValueMethod.getgene());
            writer.IfNotZero((then) =>
            {
                writer.EmitInstructionField(OpCodeNumber.Ldfld, field.GetCanonicalGenericInstance());
                writer.EmitInstructionType(OpCodeNumber.Box, field.FieldType);
                writer.EmitInstructionMethod(OpCodeNumber.Callvirt, GetHashCodeMethodReference);
            },
            (elseBranch) =>
            {
                writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
            });
            
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