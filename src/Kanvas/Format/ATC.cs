using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Interface;
using Komponent.IO;

namespace Kanvas.Format
{
    public class ATC : IColorEncoding
    {
        public bool IsBlockCompression { get => true; }

        public int BitDepth { get; set; }
        public int BlockBitDepth { get; set; }

        public string FormatName { get; set; }

        bool alpha;
        Support.ATC.AlphaMode alphaMode;
        ByteOrder byteOrder;

        public ATC(bool alpha = false, Support.ATC.AlphaMode alphaMode = Support.ATC.AlphaMode.Explicit, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            BitDepth = (alpha) ? 8 : 4;
            BlockBitDepth = (alpha) ? 128 : 64;

            this.alpha = alpha;
            this.alphaMode = alphaMode;
            this.byteOrder = byteOrder;

            FormatName = "ATC_RGB" + ((alpha) ? (alphaMode == Support.ATC.AlphaMode.Interpolated) ? "A Interpolated" : "A Explicit" : "");
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (var br = new BinaryReaderX(new MemoryStream(tex), byteOrder))
            {
                var atcdecoder = new Support.ATC.Decoder(alpha, alphaMode);

                while (true)
                {
                    yield return atcdecoder.Get(() =>
                    {
                        var Alpha = alpha ? br.ReadUInt64() : 0;
                        return (Alpha, br.ReadUInt64());
                    });
                }
            }
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var atcencoder = new Support.ATC.Encoder(alpha, alphaMode);

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms))
                foreach (var color in colors)
                    atcencoder.Set(color, data =>
                    {
                        if (alpha) bw.Write(data.alpha);
                        bw.Write(data.block);
                    });

            return ms.ToArray();
        }
    }
}
