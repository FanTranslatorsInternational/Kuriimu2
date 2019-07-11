using System.IO;

namespace Kompression.LempelZiv.Decoders
{
    public interface ILzDecoder
    {
        void Decode(Stream input,Stream output);
    }
}
