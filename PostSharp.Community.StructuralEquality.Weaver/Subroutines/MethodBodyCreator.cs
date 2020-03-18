using PostSharp.Reflection;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Utilities;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    public static class MethodBodyCreator
    {
        /// <summary>
        /// Creates a new method body and assigns it to <paramref name="hostMethod"/>. The method body looks as described in <see cref="CreatedEmptyMethod"/>.
        /// </summary>
        /// <param name="instructionWriter">A <b>detached</b> instruction writer.</param>
        /// <param name="hostMethod">The method without body. The body will be assigned to this method.</param>
        /// <returns>References to points in the method body.</returns>
        public static CreatedEmptyMethod CreateModifiableMethodBody( InstructionWriter instructionWriter, MethodDefDeclaration hostMethod )
        {
            // Create a new method body to host the pipeline.
            hostMethod.MethodBody = new MethodBodyDeclaration();
            InstructionBlock rootInstructionBlock = hostMethod.MethodBody.RootInstructionBlock = hostMethod.MethodBody.CreateInstructionBlock();

            InstructionBlock sequencePointBlock = rootInstructionBlock.AddChildBlock();
            InstructionSequence sequencePointSequence = sequencePointBlock.AddInstructionSequence();
            instructionWriter.AttachInstructionSequence( sequencePointSequence );
            instructionWriter.EmitSymbolSequencePoint( SymbolSequencePoint.Hidden );
            instructionWriter.EmitInstruction( OpCodeNumber.Nop );
            instructionWriter.DetachInstructionSequence();

            InstructionBlock implementationBlock = rootInstructionBlock.AddChildBlock();
            InstructionBlock returnBlock = rootInstructionBlock.AddChildBlock();
            InstructionSequence returnSequence = returnBlock.AddInstructionSequence();

            instructionWriter.AttachInstructionSequence( returnSequence );
            instructionWriter.EmitSymbolSequencePoint( SymbolSequencePoint.Hidden );
            LocalVariableSymbol returnVariable;
            if ( !hostMethod.ReturnParameter.ParameterType.IsIntrinsic( IntrinsicType.Void ) )
            {
                hostMethod.MethodBody.InitLocalVariables = true;
                returnVariable = rootInstructionBlock.DefineLocalVariable(
                    hostMethod.ReturnParameter.ParameterType,
                    DebuggerSpecialNames.GetVariableSpecialName(hostMethod.Domain , "returnValue", DebuggerSpecialVariableKind.ReturnValue)
                    );
                instructionWriter.EmitInstructionLocalVariable( OpCodeNumber.Ldloc, returnVariable );
            }
            else
            {
                returnVariable = null;
            }

            instructionWriter.EmitInstruction( OpCodeNumber.Ret );
            instructionWriter.DetachInstructionSequence();
            return new CreatedEmptyMethod( hostMethod, implementationBlock, returnVariable, returnSequence );
        }
    }
}