using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Interface;
using System.Drawing;
using Komponent.IO;
using System.IO;

namespace Kanvas.Format
{
    public class ATI : IColorEncoding
    {
        public enum Format
        {
            ATI1L,
            ATI1A,
            ATI2,
        }

        public bool IsBlockCompression { get => true; }

        public int BitDepth { get; set; }
        public int BlockBitDepth { get; set; }

        public string FormatName { get; set; }

        Format format;

        ByteOrder byteOrder;

        public ATI(Format format, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            BitDepth = (format == Format.ATI1L || format == Format.ATI1A) ? 4 : 8;
            BlockBitDepth = (format == Format.ATI1L || format == Format.ATI1A) ? 64 : 128;

            this.format = format;
            FormatName = format.ToString();

            this.byteOrder = byteOrder;
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using (var br = new BinaryReaderX(new MemoryStream(tex), byteOrder))
            {
                Enum.TryParse<Support.ATI.Format>(format.ToString(), false, out var atiformat);
                var atidecoder = new Support.ATI.Decoder(atiformat);

                while (true)
                {
                    yield return atidecoder.Get(() =>
                    {
                        //if (br.BaseStream.Position == br.BaseStream.Length) return (0, 0);
                        return (br.ReadUInt64(), format == Format.ATI2 ? br.ReadUInt64() : 0);
                    });
                }
            }
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            Enum.TryParse<Support.ATI.Format>(format.ToString(), false, out var atiFormat);
            var atiencoder = new Support.ATI.Encoder(atiFormat);

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms))
                foreach (var color in colors)
                    atiencoder.Set(color, data =>
                    {
                        bw.Write(data.block);
                        if (format == Format.ATI2) bw.Write(data.alpha);
                    });

            return ms.ToArray();
        }
    }
}
