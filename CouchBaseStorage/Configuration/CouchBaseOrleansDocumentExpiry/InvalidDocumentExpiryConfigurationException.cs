using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CouchBaseProviders.Configuration.CouchbaseOrleansDocumentExpiry
{
    internal class InvalidDocumentExpiryConfigurationException : AggregateException
    {
        public InvalidDocumentExpiryConfigurationException()
        {
        }

        public InvalidDocumentExpiryConfigurationException(string message) : base(message)
        {
        }

        public InvalidDocumentExpiryConfigurationException(IEnumerable<Exception> innerExceptions) : base(innerExceptions)
        {
        }

        public InvalidDocumentExpiryConfigurationException(params Exception[] innerExceptions) : base(innerExceptions)
        {
        }

        public InvalidDocumentExpiryConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public InvalidDocumentExpiryConfigurationException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions)
        {
        }

        public InvalidDocumentExpiryConfigurationException(string message, params Exception[] innerExceptions) : base(message, innerExceptions)
        {
        }

        protected InvalidDocumentExpiryConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
