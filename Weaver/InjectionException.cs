using System;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    /// <summary> 
    /// Exceptions of this class are caught and turned to <c>Message.Write()</c> calls so that they show up as error
    /// messages.
    /// </summary>
    internal class InjectionException : Exception
    {
        public string ErrorCode { get; }

        public InjectionException( string errorCode, string message ) : base( message )
        {
            this.ErrorCode = errorCode;
        }
    }
}