using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Exceptions
{
    /// <summary>
    /// Exception thrown by the processor of Kanvas.
    /// </summary>
    [Serializable]
    public class ColorEncodingException : Exception
    {
        public ColorEncodingException() { }
        public ColorEncodingException(string message) : base(message) { }
        public ColorEncodingException(string message, Exception innerException) : base(message, innerException) { }
        protected ColorEncodingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
