using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CouchbaseProviders.Configuration.CouchbaseOrleansDocumentExpiry
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

        public static InvalidExpiryValueException Generate(IReadOnlyCollection<string> grainTypes)
        {
            var onlyOne = grainTypes.Count == 1;

            var message = new StringBuilder();
            message.AppendLine($"The document expiry value{(onlyOne ? "" : "s")} for the following grain type{(onlyOne ? " is" : "s are")} invalid: {string.Join(", ", grainTypes)}");
            message.AppendLine();
            message.AppendLine("Valid expiry values include:");
            message.AppendLine($"10 seconds: {TimeSpan.FromSeconds(10)}");
            message.AppendLine($"10 minutes: {TimeSpan.FromMinutes(10)}");
            message.AppendLine($"10 hours: {TimeSpan.FromHours(10)}");
            message.AppendLine($"10 days: {TimeSpan.FromDays(10).ToString().Replace(".", ":")}");

            return new InvalidExpiryValueException(message.ToString());
        }
    }
}
