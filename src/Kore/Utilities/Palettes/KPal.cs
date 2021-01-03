using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Komponent.Utilities;
using Kontract.Models.IO;
using Kore.Exceptions.KPal;
using Kore.Utilities.Models;

namespace Kore.Utilities.Palettes
{
    public class KPal
    {
        public static KPal FromFile(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException(fileName);

            var stream = File.OpenRead(fileName);
            if (stream.Length < 0x10)
                throw new InvalidKPalException();

            using (var br = new BinaryReader(stream))
            {
                var magic = Encoding.ASCII.GetString(br.ReadBytes(4));
                var version = Conversion.FromByteArray<int>(br.ReadBytes(4), ByteOrder.LittleEndian);
                var sig = Encoding.ASCII.GetString(br.ReadBytes(4));
                var headerSize = Conversion.FromByteArray<int>(br.ReadBytes(4), ByteOrder.LittleEndian);
                if (magic != "KPAL" || sig != "FTI\0" || headerSize != 0x1C || stream.Length < headerSize)
                    throw new InvalidKPalException();
                if (version != 1)
                    throw new UnsupportedKPalVersionException(version);

                var dataSize = Conversion.FromByteArray<int>(br.ReadBytes(4), ByteOrder.LittleEndian);
                if (stream.Length < headerSize + dataSize)
                    throw new InvalidKPalException();

                var redDepth = br.ReadByte();
                var greenDepth = br.ReadByte();
                var blueDepth = br.ReadByte();
                var alphaDepth = br.ReadByte();

                var colorCount = Conversion.FromByteArray<int>(br.ReadBytes(4), ByteOrder.LittleEndian);

                var palette = new List<Color>();
                for (int i = 0; i < colorCount; i++)
                {
                    var value = Conversion.FromByteArray<int>(br.ReadBytes(4), ByteOrder.LittleEndian);

                    var alpha = value & ((1 << alphaDepth) - 1);
                    var blue = (value >> alphaDepth) & ((1 << blueDepth) - 1);
                    var green = (value >> (alphaDepth + blueDepth)) & ((1 << greenDepth) - 1);
                    var red = (value >> (alphaDepth + blueDepth + greenDepth)) & ((1 << redDepth) - 1);

                    palette.Add(Color.FromArgb(alpha, red, green, blue));
                }

                return new KPal(palette, version, redDepth, greenDepth, blueDepth, alphaDepth);
            }
        }

        public IList<Color> Palette { get; }

        public int Version { get; }

        public byte RedDepth { get; }
        public byte GreenDepth { get; }
        public byte BlueDepth { get; }
        public byte AlphaDepth { get; }

        public KPal(IList<Color> palette, int version, byte redDepth = 8, byte greenDepth = 8, byte blueDepth = 8, byte alphaDepth = 8)
        {
            Palette = palette;
            Version = version;

            RedDepth = redDepth;
            GreenDepth = greenDepth;
            BlueDepth = blueDepth;
            AlphaDepth = alphaDepth;
        }

        public void Save(string fileName)
        {
            var header = new KPALHeader
            {
                version = Version,
                headerSize = 0x1C,
                redBitDepth = RedDepth,
                greenBitDepth = GreenDepth,
                blueBitDepth = BlueDepth,
                alphaBitDepth = AlphaDepth,
                colorCount = Palette.Count
            };
            var stream = File.Create(fileName);

            using (var bw = new BinaryWriter(stream))
            {
                stream.Position = header.headerSize;
                foreach (var c in Palette)
                {
                    int value = 0;
                    value |= Conversion.ChangeBitDepth(c.R, 8, RedDepth) << (AlphaDepth + BlueDepth + GreenDepth);
                    value |= Conversion.ChangeBitDepth(c.G, 8, GreenDepth) << (AlphaDepth + BlueDepth);
                    value |= Conversion.ChangeBitDepth(c.B, 8, BlueDepth) << AlphaDepth;
                    value |= Conversion.ChangeBitDepth(c.A, 8, AlphaDepth);

                    bw.Write(Conversion.ToByteArray(value, 4, ByteOrder.LittleEndian));
                }

                header.dataSize = (int)(stream.Length - header.headerSize);

                stream.Position = 0;
                bw.Write(Encoding.ASCII.GetBytes(header.magic));
                bw.Write(Conversion.ToByteArray(header.version, 4, ByteOrder.LittleEndian));
                bw.Write(Encoding.ASCII.GetBytes(header.devMagic));
                bw.Write(Conversion.ToByteArray(header.headerSize, 4, ByteOrder.LittleEndian));
                bw.Write(Conversion.ToByteArray(header.dataSize, 4, ByteOrder.LittleEndian));
                bw.Write(header.redBitDepth);
                bw.Write(header.greenBitDepth);
                bw.Write(header.blueBitDepth);
                bw.Write(header.alphaBitDepth);
                bw.Write(Conversion.ToByteArray(header.colorCount, 4, ByteOrder.LittleEndian));
            }
        }

        private class KPALHeader
        {
            public string magic = "KPAL";
            public int version;
            public string devMagic = "FTI\0";
            public int headerSize; // Denotes data size of the header
            public int dataSize; // Starts after header and denotes palette size
            public byte redBitDepth;
            public byte greenBitDepth;
            public byte blueBitDepth;
            public byte alphaBitDepth;
            public int colorCount;
        }
    }
}
