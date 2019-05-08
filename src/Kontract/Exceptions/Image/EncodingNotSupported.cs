using System;
using System.Runtime.Serialization;
using Kontract.Models.Image;

namespace Kontract.Exceptions.Image
{
    public class EncodingNotSupported : Exception
    {
        public EncodingInfo EncodingInfo { get; }

        public EncodingNotSupported(EncodingInfo info) : base($"The encoding {info.EncodingName} is not supported.")
        {
            EncodingInfo = info;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(EncodingInfo), EncodingInfo);
            base.GetObjectData(info, context);
        }
    }
}
