using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Encoding.Support.ATC;
using Kanvas.Encoding.Support.ATC.Models;
using Kanvas.Interface;
using Kanvas.Models;
using Kanvas.Support;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the ATC encoding.
    /// </summary>
    public class ATC : IColorEncoding
    {
        private readonly AlphaMode _alphaMode;

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

        public ATC(AlphaMode alphaMode)
        {
            BitDepth = (alphaMode != AlphaMode.None) ? 8 : 4;
            BlockBitDepth = (alphaMode != AlphaMode.None) ? 128 : 64;

            _alphaMode = alphaMode;

            FormatName = "ATC_RGB" + ((alphaMode != AlphaMode.None) ? (alphaMode == AlphaMode.Interpolated) ? "A Interpolated" : "A Explicit" : "");
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            var atcDecoder = new Decoder(_alphaMode);

            using (var br = new BinaryReader(new MemoryStream(tex)))
                while (true)
                    yield return atcDecoder.Get(() =>
                    {
                        var alpha = _alphaMode != AlphaMode.None ? Convert.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder) : 0;
                        return (alpha, Convert.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder));
                    });
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var atcEncoder = new Encoder(_alphaMode);

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.ASCII, true))
                foreach (var color in colors)
                    atcEncoder.Set(color, data =>
                    {
                        if (_alphaMode != AlphaMode.None) bw.Write(Convert.ToByteArray(data.alpha, 8, ByteOrder));
                        bw.Write(Convert.ToByteArray(data.block, 8, ByteOrder));
                    });

            return ms.ToArray();
        }
    }
}
