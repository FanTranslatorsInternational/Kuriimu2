using System.Runtime.InteropServices;
using PVRTexLib;
using SixLabors.ImageSharp;

namespace Kanvas.Encoding.BlockCompression.Pvr
{
    class PvrTextureWrapper
    {
        public const ulong RGBA8888 = 0x0808080861626772ul;

        public static PVRTexture? CreateTexture(byte[] tex, PVRTexLibPixelFormat format, Size size)
            => CreateTexture(tex, (ulong)format, size);

        public static unsafe PVRTexture? CreateTexture(byte[] tex, ulong format, Size size)
        {
            GCHandle dataPtr = GCHandle.Alloc(tex, GCHandleType.Pinned);

            var header = new PVRTextureHeader(format, (uint)size.Width, (uint)size.Height,
                colourSpace: PVRTexLibColourSpace.Linear, channelType: PVRTexLibVariableType.UnsignedByteNorm);
            var texture = new PVRTexture(header, (void*)dataPtr.AddrOfPinnedObject());

            dataPtr.Free();

            return texture;
        }

        public static unsafe byte[] GetData(PVRTexture texture)
        {
            var dataPtr = (nint)texture.GetTextureDataPointer();
            ulong dataSize = texture.GetTextureDataSize(0);

            var data = new byte[dataSize];
            Marshal.Copy(dataPtr, data, 0, data.Length);

            return data;
        }
    }
}
