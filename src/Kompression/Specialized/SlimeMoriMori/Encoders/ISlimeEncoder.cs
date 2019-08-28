using System.IO;
using Kompression.IO;
using Kompression.PatternMatch;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    interface ISlimeEncoder
    {
        void Encode(Stream input, BitWriter bw, Match[] matches);
    }
}
