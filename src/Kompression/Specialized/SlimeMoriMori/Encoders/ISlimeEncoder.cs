using System.IO;
using Kompression.IO;
using Kompression.LempelZiv;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    interface ISlimeEncoder
    {
        void Encode(Stream input, BitWriter bw, LzMatch[] matches);
    }
}
