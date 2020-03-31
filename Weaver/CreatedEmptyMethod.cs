using PostSharp.Sdk.CodeModel;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    /// <summary>
    /// Represents a newly created empty method and contains points in the method that callers might want to use.
    /// The method is of the form
    /// <code>
    /// {
    ///    var returnVariable;
    ///    principal-block: {
    ///
    ///    }
    ///    return-sequence:
    ///     return returnVariable;
    /// }
    /// </code>
    /// </summary>
    public class CreatedEmptyMethod
    {
        /// <summary>
        /// Gets the created method.
        /// </summary>
        public MethodDefDeclaration MethodDeclaration { get; }
        /// <summary>
        /// Gets the instruction block that's before <see cref="ReturnSequence"/>.
        /// </summary>
        public InstructionBlock PrincipalBlock { get; }
        /// <summary>
        /// Gets the variable that the principal block should assign before ending. It will be returned by the return sequence.
        /// </summary>
        public LocalVariableSymbol ReturnVariable { get; }
        /// <summary>
        /// Gets the label to the filled-in sequence that returns <see cref="ReturnVariable"/> using <c>ldloc</c> and <c>ret</c>. 
        /// </summary>
        public InstructionSequence ReturnSequence { get; }

        public CreatedEmptyMethod( MethodDefDeclaration methodDeclaration, InstructionBlock principalBlock, LocalVariableSymbol returnVariable, InstructionSequence returnSequence )
        {
            this.MethodDeclaration = methodDeclaration;
            this.PrincipalBlock = principalBlock;
            this.ReturnVariable = returnVariable;
            this.ReturnSequence = returnSequence;
        }
    }
}