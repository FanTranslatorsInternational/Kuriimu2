using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Kontract.Models.Image;

namespace Kontract.Exceptions.Image
{
    public class IndexedEncodingNotSupported : Exception
    {
        public EncodingInfo IndexedEncodingInfo { get; }

        public IndexedEncodingNotSupported(EncodingInfo info) : base($"The indexed encoding {info.EncodingName} is not supported.")
        {
            IndexedEncodingInfo = info;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(IndexedEncodingInfo), IndexedEncodingInfo);
            base.GetObjectData(info, context);
        }
    }
}
