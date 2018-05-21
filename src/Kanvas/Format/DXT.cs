using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Interface;
using Komponent.IO;

namespace Kanvas.Format
{
    public class DXT : IImageFormat
    {
        public enum Version
        {
            DXT1,
            DXT3,
            DXT5
        }

        public int BitDepth { get; set; }
        public int BlockBitDepth { get; set; }
        public string FormatName { get; set; }

        Version version;
        ByteOrder byteOrder;

        public DXT(Version version, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            BitDepth = (version == Version.DXT1) ? 4 : 8;
            BlockBitDepth = (version == Version.DXT1) ? 64 : 128;

            this.version = version;
            this.byteOrder = byteOrder;

            FormatName = version.ToString();
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (var br = new BinaryReaderX(new MemoryStream(tex), byteOrder))
            {
                Enum.TryParse<Support.DXT.Formats>(version.ToString(), false, out var dxtFormat);
                var dxtdecoder = new Support.DXT.Decoder(dxtFormat);

                while (true)
                {
                    yield return dxtdecoder.Get(() =>
                    {
                        var dxt5Alpha = version == Version.DXT3 || version == Version.DXT5 ? br.ReadUInt64() : 0;
                        return (dxt5Alpha, br.ReadUInt64());
                    });
                }
            }
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            Enum.TryParse<Support.DXT.Formats>(version.ToString(), false, out var dxtFormat);
            var dxtencoder = new Support.DXT.Encoder(dxtFormat);

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms))
                foreach (var color in colors)
                    dxtencoder.Set(color, data =>
                    {
                        if (version == Version.DXT5) bw.Write(data.alpha);
                        bw.Write(data.block);
                    });

            return ms.ToArray();
        }
    }
}
