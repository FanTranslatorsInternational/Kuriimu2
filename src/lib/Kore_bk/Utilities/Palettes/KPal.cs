using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Komponent.Utilities;
using Kore.Exceptions.KPal;

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
                var version = BinaryPrimitives.ReadInt32LittleEndian(br.ReadBytes(4));
                var sig = Encoding.ASCII.GetString(br.ReadBytes(4));
                var headerSize = BinaryPrimitives.ReadInt32LittleEndian(br.ReadBytes(4));
                if (magic != "KPAL" || sig != "FTI\0" || headerSize != 0x1C || stream.Length < headerSize)
                    throw new InvalidKPalException();
                if (version != 1)
                    throw new UnsupportedKPalVersionException(version);

                var dataSize = BinaryPrimitives.ReadInt32LittleEndian(br.ReadBytes(4));
                if (stream.Length < headerSize + dataSize)
                    throw new InvalidKPalException();

                var redDepth = br.ReadByte();
                var greenDepth = br.ReadByte();
                var blueDepth = br.ReadByte();
                var alphaDepth = br.ReadByte();

                var colorCount = BinaryPrimitives.ReadInt32LittleEndian(br.ReadBytes(4));

                var palette = new List<Color>();
                for (int i = 0; i < colorCount; i++)
                {
                    var value = BinaryPrimitives.ReadInt32LittleEndian(br.ReadBytes(4));

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

            var buffer = new byte[4];
            using (var bw = new BinaryWriter(stream))
            {
                stream.Position = header.headerSize;
                foreach (var c in Palette)
                {
                    var value = 0;
                    value |= ChangeBitDepth(c.R, RedDepth) << (AlphaDepth + BlueDepth + GreenDepth);
                    value |= ChangeBitDepth(c.G, GreenDepth) << (AlphaDepth + BlueDepth);
                    value |= ChangeBitDepth(c.B, BlueDepth) << AlphaDepth;
                    value |= ChangeBitDepth(c.A, AlphaDepth);

                    BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
                    bw.Write(buffer);
                }

                header.dataSize = (int)(stream.Length - header.headerSize);

                stream.Position = 0;
                bw.Write(Encoding.ASCII.GetBytes(header.magic));
                BinaryPrimitives.WriteInt32LittleEndian(buffer, header.version);
                bw.Write(buffer);
                bw.Write(Encoding.ASCII.GetBytes(header.devMagic));
                BinaryPrimitives.WriteInt32LittleEndian(buffer, header.headerSize);
                bw.Write(buffer);
                BinaryPrimitives.WriteInt32LittleEndian(buffer, header.dataSize);
                bw.Write(buffer);
                bw.Write(header.redBitDepth);
                bw.Write(header.greenBitDepth);
                bw.Write(header.blueBitDepth);
                bw.Write(header.alphaBitDepth);
                BinaryPrimitives.WriteInt32LittleEndian(buffer, header.colorCount);
                bw.Write(buffer);
            }
        }

        private int ChangeBitDepth(int value, int depth)
        {
            if (depth == 0)
                return 0;

            if (depth > 8)
                return Conversion.DownscaleBitDepth(value, depth, 8);

            return Conversion.UpscaleBitDepth(value, depth);
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
