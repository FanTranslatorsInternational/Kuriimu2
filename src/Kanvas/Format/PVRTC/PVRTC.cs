using System.Collections.Generic;
using System.Text;
using Kanvas.Interface;
using System.Drawing;
using System.IO;
using Kanvas.Format.PVRTC.Models;

namespace Kanvas.Format.PVRTC
{
    /// <summary>
    /// Defines the PVRTC encoding.
    /// </summary>
    public class PVRTC : IColorTranscodingKnownDimensions
    {
        private readonly PvrtcFormat _format;

        /// <inheritdoc cref="IColorTranscoding.BitDepth"/>
        public int BitDepth { get; }

        /// <summary>
        /// The number of bits one block contains of.
        /// </summary>
        public int BlockBitDepth { get; }

        /// <inheritdoc cref="IColorTranscoding.FormatName"/>
        public string FormatName { get; }

        /// <inheritdoc cref="IColorTranscoding.IsBlockCompression"/>
        public bool IsBlockCompression => true;

        /// <inheritdoc cref="IColorTranscodingKnownDimensions.Width"/>
        public int Width { private get; set; } = -1;

        /// <inheritdoc cref="IColorTranscodingKnownDimensions.Height"/>
        public int Height { private get; set; } = -1;

        public PVRTC(PvrtcFormat format)
        {
            BitDepth = (format == PvrtcFormat.PVRTCA_2bpp || format == PvrtcFormat.PVRTC_2bpp || format == PvrtcFormat.PVRTC2_2bpp) ? 2 : 4;
            BlockBitDepth = (format == PvrtcFormat.PVRTCA_2bpp || format == PvrtcFormat.PVRTC_2bpp || format == PvrtcFormat.PVRTC2_2bpp) ? 32 : 64;

            _format = format;

            FormatName = format.ToString();
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            if (Width <= 0 || Height <= 0)
                throw new InvalidDataException("Height and Width has to be set for PVRTC.");

            var pvrtcTex = PVRTexture.CreateTexture(tex, (uint)Width, (uint)Height, 1, (PixelFormat)_format, false, VariableType.UnsignedByte, ColorSpace.lRGB);

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

        public byte[] Save(IEnumerable<Color> colors)
        {
            if (Width <= 0 || Height <= 0)
                throw new InvalidDataException("Height and Width has to be set for PVRTC.");

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.ASCII, true))
                foreach (var color in colors)
                {
                    bw.Write(color.R);
                    bw.Write(color.G);
                    bw.Write(color.B);
                    bw.Write(color.A);
                }

            var pvrtcTex = PVRTexture.CreateTexture(ms.ToArray(), (uint)Width, (uint)Height, 1, PixelFormat.RGBA8888, false, VariableType.UnsignedByteNorm, ColorSpace.lRGB);

            pvrtcTex.Transcode((PixelFormat)_format, VariableType.UnsignedByteNorm, ColorSpace.lRGB, CompressorQuality.PVRTCHigh);

            byte[] encodedTex = new byte[pvrtcTex.GetTextureDataSize()];
            pvrtcTex.GetTextureData(encodedTex, pvrtcTex.GetTextureDataSize());

            return encodedTex;
        }
    }
}
