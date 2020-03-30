using System;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    // TODO: Should be changed to Message.Write or AssertionFailedException, whether it's the user's fault or not.
    
    internal class InjectionException : Exception
    {
        public string ErrorCode { get; }

        public InjectionException( string errorCode, string message ) : base( message )
        {
            this.ErrorCode = errorCode;
        }
    }
}