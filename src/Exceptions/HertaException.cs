
using System;

namespace Herta.Exceptions.HertaException
{
    public class HertaBaseException : Exception
    // base class for all HertaApi exceptions
    {
        public HertaBaseException(string message, Exception? innerException) : base(message, innerException) { }
    }
}
