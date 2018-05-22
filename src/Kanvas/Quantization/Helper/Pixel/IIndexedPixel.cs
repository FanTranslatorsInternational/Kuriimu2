using System;

namespace Kanvas.Quantization.Helper.Pixels
{
    public interface IIndexedPixel
    {
        // index methods
        Byte GetIndex(Int32 offset);
        void SetIndex(Int32 offset, Byte value);
    }
}
