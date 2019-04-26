using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Kanvas.Format.ETC1.Models;
using Kanvas.Interface;
using Kanvas.Models;
using Kanvas.Support;

namespace Kanvas.Format.ETC1
{
    /// <summary>
    /// Defines the ETC1 encoding.
    /// </summary>
    public class ETC1 : IColorTranscoding
    {
        private bool _useAlpha;

        /// <inheritdoc cref="IColorTranscoding.BitDepth"/>
        public int BitDepth { get; }

        /// <summary>
        /// The number of bits one block contains of.
        /// </summary>
        public int BlockBitDepth { get; }

        /// <inheritdoc cref="IColorTranscoding.FormatName"/>
        public string FormatName { get; set; }

        /// <inheritdoc cref="IColorTranscoding.IsBlockCompression"/>
        public bool IsBlockCompression => true;

        /// <summary>
        /// Byte order to use to read the values.
        /// </summary>
        public ByteOrder ByteOrder { get; set; } = ByteOrder.LittleEndian;

        /// <summary>
        /// Defines if useAlpha should be used.
        /// </summary>
        public bool UseAlpha
        {
            get => _useAlpha;
            set
            {
                _useAlpha = value;
                UpdateName();
            }
        }

        /// <summary>
        /// Defines if ZOrder is used.
        /// </summary>
        public bool UseZOrder { get; set; }

        public ETC1(bool useAlpha)
        {
            BitDepth = useAlpha ? 8 : 4;
            BlockBitDepth = useAlpha ? 128 : 64;

            UseAlpha = useAlpha;

            UpdateName();
        }

        private void UpdateName()
        {
            FormatName = (UseAlpha) ? "ETC1A4" : "ETC1";
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            var etc1Decoder = new Decoder(UseZOrder);

            using (var br = new BinaryReader(new MemoryStream(tex)))
                while (true)
                    yield return etc1Decoder.Get(() => GetPixelData(br));
        }

        private PixelData GetPixelData(BinaryReader br)
        {
            var etc1Alpha = UseAlpha ? Convert.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder) : ulong.MaxValue;
            var colorBlock = Convert.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder);
            var etc1Block = new Block
            {
                LSB = (ushort)(colorBlock & 0xFFFF),
                MSB = (ushort)((colorBlock >> 16) & 0xFFFF),
                Flags = (byte)((colorBlock >> 32) & 0xFF),
                B = (byte)((colorBlock >> 40) & 0xFF),
                G = (byte)((colorBlock >> 48) & 0xFF),
                R = (byte)((colorBlock >> 56) & 0xFF)
            };
            return new PixelData { Alpha = etc1Alpha, Block = etc1Block };
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var etc1Encoder = new Encoder(UseZOrder);

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                foreach (var color in colors)
                    etc1Encoder.Set(color, data => SetPixelData(bw, data));
            }

            return ms.ToArray();
        }

        private void SetPixelData(BinaryWriter bw, PixelData data)
        {
            if (UseAlpha)
                bw.Write(Convert.ToByteArray(data.Alpha, 8, ByteOrder));

            ulong colorBlock = 0;
            colorBlock |= data.Block.LSB;
            colorBlock |= ((ulong)data.Block.MSB << 16);
            colorBlock |= ((ulong)data.Block.Flags << 32);
            colorBlock |= ((ulong)data.Block.B << 40);
            colorBlock |= ((ulong)data.Block.G << 48);
            colorBlock |= ((ulong)data.Block.R << 56);

            bw.Write(Convert.ToByteArray(colorBlock, 8, ByteOrder));
        }
    }
}
