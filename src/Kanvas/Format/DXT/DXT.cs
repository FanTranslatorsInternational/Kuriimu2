using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Kanvas.Format.DXT.Models;
using Kanvas.Interface;
using Kanvas.Models;
using Kanvas.Support;

namespace Kanvas.Format.DXT
{
    /// <summary>
    /// Defines the Dxt encoding.
    /// </summary>
    public class DXT : IColorTranscoding
    {
        private readonly DxtFormat _format;

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

        public DXT(DxtFormat format)
        {
            BitDepth = (format == DxtFormat.DXT1) ? 4 : 8;
            BlockBitDepth = (format == DxtFormat.DXT1) ? 64 : 128;

            _format = format;

            FormatName = format.ToString();
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            var dxtDecoder = new Decoder(_format);

            using (var br = new BinaryReader(new MemoryStream(tex)))
                while (true)
                    yield return dxtDecoder.Get(() =>
                    {
                        var dxt5Alpha = _format == DxtFormat.DXT3 || _format == DxtFormat.DXT5 ? Convert.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder) : 0;
                        return (dxt5Alpha, Convert.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder));
                    });
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var dxtEncoder = new Encoder(_format);

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.ASCII, true))
                foreach (var color in colors)
                    dxtEncoder.Set(color, data =>
                    {
                        if (_format == DxtFormat.DXT5)
                            bw.Write(Convert.ToByteArray(data.alpha, 8, ByteOrder));
                        bw.Write(Convert.ToByteArray(data.block, 8, ByteOrder));
                    });

            return ms.ToArray();
        }
    }
}
