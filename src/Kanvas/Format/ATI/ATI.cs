using System.Collections.Generic;
using System.Text;
using Kanvas.Interface;
using System.Drawing;
using System.IO;
using Kanvas.Format.ATI.Models;
using Kanvas.Models;
using Kanvas.Support;

namespace Kanvas.Format.ATI
{
    /// <summary>
    /// Defines the ATI encoding.
    /// </summary>
    public class ATI : IColorTranscoding
    {
        private readonly AtiFormat _format;

        /// <inheritdoc cref="IColorTranscoding.IsBlockCompression"/>
        public bool IsBlockCompression => true;

        /// <inheritdoc cref="IColorTranscoding.BitDepth"/>
        public int BitDepth { get; }

        /// <summary>
        /// The number of bits one block contains of.
        /// </summary>
        public int BlockBitDepth { get; }

        /// <inheritdoc cref="IColorTranscoding.FormatName"/>
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
                    yield return atiDecoder.Get(() => (Convert.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder),
                        _format == AtiFormat.ATI2 ? Convert.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder) : 0));
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var atiEncoder = new Encoder(_format);

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.ASCII, true))
                foreach (var color in colors)
                    atiEncoder.Set(color, data =>
                    {
                        bw.Write(Convert.ToByteArray(data.block, 8, ByteOrder));
                        if (_format == AtiFormat.ATI2)
                            bw.Write(Convert.ToByteArray(data.alpha, 8, ByteOrder));
                    });

            return ms.ToArray();
        }
    }
}
