using System;
using System.Collections.Generic;
using System.Reflection;
using PostSharp.Community.DeepSerializable;
using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Configuration;
using PostSharp.Sdk.Extensibility.Tasks;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    [ExportTask(Phase = TaskPhase.CustomTransform, TaskName = nameof(HelloWorldTask))] 
     // The [RequirePostSharp] attribute on HelloWorldAttribute causes PostSharp to look through assemblies on its search path
     // for assemblies named PostSharp.Community.StructuralEquality.Weaver.dll which contain an exported task named HelloWorldTask.
     // We're working in the CustomTransform phase, which happens after all other transformations that PostSharp runs. 
    [TaskDependency("AnnotationRepository", IsRequired = true, Position = DependencyPosition.Before)]
     // This allows us to safely use GetService<IAnnotationRepositoryService> to get the methods annotated with our 
     // attribute. In practice, almost every PostSharp tasks requires the annotation repository, so our task is highly 
     // likely to work even without this line, except if we were working in the Analyze phase.
    public class HelloWorldTask : Task
    {
        public override bool Execute()
        {
            var consoleWriteLine = FindConsoleWriteLine();
            
            var annotationRepositoryService = this.Project.GetService<IAnnotationRepositoryService>();
            var enumerator = annotationRepositoryService.GetAnnotationsOfType(typeof(HelloWorldAttribute), false, false);
            while (enumerator.MoveNext())
            {
                // Iterates over declarations to which our attribute has been applied. If the attribute weren't
                // a MulticastAttribute, that would be just the declarations that it annotates. With multicasting, it 
                // can be far more declarations.
                
                MetadataDeclaration targetDeclaration = enumerator.Current.TargetElement;
                MethodDefDeclaration targetMethod = (MethodDefDeclaration) targetDeclaration;
                // Multicasting ensures that our attribute is only applied to methods, so there is little chance of 
                // a class cast error here. 
                
                AddHelloWorldToMethod(targetMethod, consoleWriteLine);
            }

            return true;
        }

        private IMethod FindConsoleWriteLine()
        {
            ModuleDeclaration module = this.Project.Module;
             // Represents the module (= assembly) that we're modifying.
             
            INamedType console = (INamedType) module.FindType(typeof(Console));
             // Finds the System.Console type usable in that module. We don't know exactly where it comes from. It could
             // be mscorlib on .NET Framework or something else on .NET Core. 
             
            IGenericMethodDefinition method = module.FindMethod(console, "WriteLine", (mdd) => mdd.Parameters.Count == 1 &&
                                                                                               mdd.Parameters[0].ParameterType
                                                                                                   .GetReflectionName() ==
                                                                                               "System.String");
             // Finds the one overload that we want: System.Console.WriteLine(System.String).
             
            return method;
        }

        private static void AddHelloWorldToMethod(MethodDefDeclaration targetMethod, IMethod consoleWriteLine)
        {
            InstructionBlock originalCode = targetMethod.MethodBody.RootInstructionBlock;
            originalCode.Detach();
             // Removes the original code from the method body. Without this, you would get exceptions.
            
            InstructionBlock root = targetMethod.MethodBody.CreateInstructionBlock();
            targetMethod.MethodBody.RootInstructionBlock = root;
             // Replaces the method body's content.
            
            InstructionBlock helloWorldBlock = root.AddChildBlock();
            InstructionSequence helloWorldSequence = helloWorldBlock.AddInstructionSequence();
            using (var writer = InstructionWriter.GetInstance())
            {
                // Add instruction to the beginning of the method body:
                writer.AttachInstructionSequence(helloWorldSequence);
                writer.EmitInstructionString(OpCodeNumber.Ldstr, new LiteralString("Hello, world!")); 
                 // This automatically adds the string to the assembly's string database.
                writer.EmitInstructionMethod(OpCodeNumber.Call, consoleWriteLine);
                writer.DetachInstructionSequence();
            }

            root.AddChildBlock(originalCode);
             // Re-adding the original code at the end.
        }
    }
}