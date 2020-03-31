using System;

namespace PostSharp.Community.StructuralEquality.Weaver
{
    internal class InjectionException : Exception
    {
        public string ErrorCode { get; }

        public InjectionException( string errorCode, string message ) : base( message )
        {
            this.ErrorCode = errorCode;
        }
    }
}