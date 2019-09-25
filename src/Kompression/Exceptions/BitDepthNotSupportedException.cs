using System;
using System.Runtime.Serialization;

namespace Kompression.Exceptions
{
    [Serializable]
    public class BitDepthNotSupportedException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public BitDepthNotSupportedException(int bitDepth) : base($"Bitdepth {bitDepth} not supported.")
        {
            Data.Add("BitDepth", bitDepth);
        }

        public BitDepthNotSupportedException(string message) : base(message)
        {
        }

        public BitDepthNotSupportedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BitDepthNotSupportedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
