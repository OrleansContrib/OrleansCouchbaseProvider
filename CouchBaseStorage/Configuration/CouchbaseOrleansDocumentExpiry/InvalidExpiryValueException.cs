using System;
using System.Runtime.Serialization;

namespace CouchBaseProviders.Configuration.CouchbaseOrleansDocumentExpiry
{
    internal class InvalidExpiryValueException : Exception
    {
        public InvalidExpiryValueException()
        {
        }

        public InvalidExpiryValueException(string message) : base(message)
        {
        }

        public InvalidExpiryValueException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidExpiryValueException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
