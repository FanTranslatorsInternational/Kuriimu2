using System.IO;
using Komponent.IO;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    interface ISlimeEncoder
    {
        void Encode(Stream input, BitWriter bw, Match[] matches);
    }
}
