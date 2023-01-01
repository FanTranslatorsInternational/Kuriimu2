using System.IO;

namespace Kompression.Specialized.SlimeMoriMori.Decoders
{
    interface ISlimeDecoder
    {
        void Decode(Stream input, Stream output);
    }
}
