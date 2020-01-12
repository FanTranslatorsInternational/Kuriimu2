using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Encoding.BlockCompressions.ATI;
using Kanvas.Encoding.BlockCompressions.ATI.Models;
using Kanvas.Support;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the ATI encoding.
    /// </summary>
    public class ATI : IColorEncoding
    {
        private readonly AtiFormat _format;

        /// <inheritdoc cref="IColorEncoding.IsBlockCompression"/>
        public bool IsBlockCompression => true;

        /// <inheritdoc cref="IColorEncoding.BitDepth"/>
        public int BitDepth { get; }

        /// <summary>
        /// The number of bits one block contains of.
        /// </summary>
        public int BlockBitDepth { get; }

        /// <inheritdoc cref="IColorEncoding.FormatName"/>
        public string FormatName { get; }

        /// <summary>
        /// Byte order to use to read the values.
        /// </summary>
        public ByteOrder ByteOrder { get; set; } = ByteOrder.LittleEndian;

        public ATI(AtiFormat format)
        {
            BitDepth = (format == AtiFormat.ATI1A || format == AtiFormat.ATI1A) ? 4 : 8;
            BlockBitDepth = (format == AtiFormat.ATI1L || format == AtiFormat.ATI1A) ? 64 : 128;

            _format = format;
            FormatName = format.ToString();
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            var atiDecoder = new Decoder(_format);

            using (var br = new BinaryReader(new MemoryStream(tex)))
                while (true)
                    yield return atiDecoder.Get(() => (Conversion.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder),
                        _format == AtiFormat.ATI2 ? Conversion.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder) : 0));
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var atiEncoder = new Encoder(_format);

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.ASCII, true))
                foreach (var color in colors)
                    atiEncoder.Set(color, data =>
                    {
                        bw.Write(Conversion.ToByteArray(data.block, 8, ByteOrder));
                        if (_format == AtiFormat.ATI2)
                            bw.Write(Conversion.ToByteArray(data.alpha, 8, ByteOrder));
                    });

            return ms.ToArray();
        }
    }
}
