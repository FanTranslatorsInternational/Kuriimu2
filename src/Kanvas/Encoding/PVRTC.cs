using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Encoding.BlockCompressions.PVRTC;
using Kanvas.Encoding.BlockCompressions.PVRTC.Models;
using Kontract.Kanvas;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the PVRTC encoding.
    /// </summary>
    public class PVRTC : IColorEncoding
    {
        private readonly PvrtcFormat _format;
        private readonly int _width;
        private readonly int _height;

        /// <inheritdoc cref="IColorEncoding.BitDepth"/>
        public int BitDepth { get; }

        public int BitsPerValue { get; }

        public int ColorsPerValue => 16;

        /// <inheritdoc cref="IColorEncoding.FormatName"/>
        public string FormatName { get; }

        public PVRTC(PvrtcFormat format, int width, int height)
        {
            BitDepth = BitsPerValue = format == PvrtcFormat.PVRTCA_2bpp || format == PvrtcFormat.PVRTC_2bpp || format == PvrtcFormat.PVRTC2_2bpp ? 32 : 64;

            _format = format;
            _width = width;
            _height = height;

            FormatName = format.ToString();
        }

        public IEnumerable<Color> Load(byte[] tex, int taskCount)
        {
            var pvrtcTex = PVRTexture.CreateTexture(tex, (uint)_width, (uint)_height, 1, (PixelFormat)_format, false, VariableType.UnsignedByte, ColorSpace.lRGB);

            pvrtcTex.Transcode(PixelFormat.RGBA8888, VariableType.UnsignedByteNorm, ColorSpace.lRGB);

            byte[] decodedTex = new byte[pvrtcTex.GetTextureDataSize()];
            pvrtcTex.GetTextureData(decodedTex, pvrtcTex.GetTextureDataSize());

            using (var br = new BinaryReader(new MemoryStream(decodedTex)))
            {
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    var v0 = br.ReadByte();
                    var v1 = br.ReadByte();
                    var v2 = br.ReadByte();
                    var v3 = br.ReadByte();
                    yield return Color.FromArgb(v3, v0, v1, v2);
                }
            }
        }

        public byte[] Save(IEnumerable<Color> colors, int taskCount)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.ASCII, true))
                foreach (var color in colors)
                {
                    bw.Write(color.R);
                    bw.Write(color.G);
                    bw.Write(color.B);
                    bw.Write(color.A);
                }

            var pvrtcTex = PVRTexture.CreateTexture(ms.ToArray(), (uint)_width, (uint)_height, 1, PixelFormat.RGBA8888, false, VariableType.UnsignedByteNorm, ColorSpace.lRGB);

            pvrtcTex.Transcode((PixelFormat)_format, VariableType.UnsignedByteNorm, ColorSpace.lRGB, CompressorQuality.PVRTCHigh);

            byte[] encodedTex = new byte[pvrtcTex.GetTextureDataSize()];
            pvrtcTex.GetTextureData(encodedTex, pvrtcTex.GetTextureDataSize());

            return encodedTex;
        }
    }
}
