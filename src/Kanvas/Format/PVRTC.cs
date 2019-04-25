using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Interface;
using System.Drawing;
using Komponent.IO;
using System.IO;
using static Kanvas.Support.PVRTC;

namespace Kanvas.Format
{
    public class PVRTC : IColorEncodingKnownDimensions
    {
        public enum Format : ulong
        {
            PVRTC_2bpp = PixelFormat.PVRTCI_2bpp_RGB,
            PVRTCA_2bpp = PixelFormat.PVRTCI_2bpp_RGBA,
            PVRTC_4bpp = PixelFormat.PVRTCI_4bpp_RGB,
            PVRTCA_4bpp = PixelFormat.PVRTCI_4bpp_RGBA,
            PVRTC2_2bpp = PixelFormat.PVRTCII_2bpp,
            PVRTC2_4bpp = PixelFormat.PVRTCII_4bpp,
        }

        public int BitDepth { get; set; }
        public int BlockBitDepth { get; set; }

        public string FormatName { get; set; }

        public bool IsBlockCompression { get => true; }
        public int Width { private get; set; } = -1;
        public int Height { private get; set; } = -1;

        Format _format;

        ByteOrder byteOrder;

        public PVRTC(Format format, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            BitDepth = (format == Format.PVRTCA_2bpp || format == Format.PVRTC_2bpp || format == Format.PVRTC2_2bpp) ? 2 : 4;
            BlockBitDepth = (format == Format.PVRTCA_2bpp || format == Format.PVRTC_2bpp || format == Format.PVRTC2_2bpp) ? 32 : 64;

            _format = format;

            FormatName = format.ToString();

            this.byteOrder = byteOrder;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            if (Width < 0 || Height < 0)
                throw new InvalidDataException("Height and Width has to be set for PVRTC.");

            var pvrtcTex = PVRTexture.CreateTexture(tex, (uint)Width, (uint)Height, 1, (PixelFormat)_format, false, VariableType.UnsignedByte, ColourSpace.lRGB);

            pvrtcTex.Transcode(PixelFormat.RGBA8888, VariableType.UnsignedByteNorm, ColourSpace.lRGB);

            byte[] decodedTex = new byte[pvrtcTex.GetTextureDataSize()];
            pvrtcTex.GetTextureData(decodedTex, pvrtcTex.GetTextureDataSize());

            using (var br = new BinaryReaderX(new MemoryStream(decodedTex)))
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
            if (Width < 0 || Height < 0)
                throw new InvalidDataException("Height and Width has to be set for PVRTC.");

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true))
                foreach (var color in colors)
                {
                    bw.Write(color.R);
                    bw.Write(color.G);
                    bw.Write(color.B);
                    bw.Write(color.A);
                }

            var pvrtcTex = PVRTexture.CreateTexture(ms.ToArray(), (uint)Width, (uint)Height, 1, PixelFormat.RGBA8888, false, VariableType.UnsignedByteNorm, ColourSpace.lRGB);

            pvrtcTex.Transcode((PixelFormat)_format, VariableType.UnsignedByteNorm, ColourSpace.lRGB, CompressorQuality.PVRTCHigh);

            byte[] encodedTex = new byte[pvrtcTex.GetTextureDataSize()];
            pvrtcTex.GetTextureData(encodedTex, pvrtcTex.GetTextureDataSize());

            return encodedTex;
        }
    }
}
