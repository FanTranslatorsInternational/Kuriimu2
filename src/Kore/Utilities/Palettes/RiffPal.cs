using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Kore.Exceptions.RiffPal;

namespace Kore.Utilities.Palettes
{
    public class RiffPal
    {
        public static RiffPal FromFile(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException(fileName);

            var stream = File.OpenRead(fileName);
            if (stream.Length < 0x8)
                throw new InvalidRiffPalException();

            using (var br = new BinaryReader(stream))
            {
                var fourCC = Encoding.ASCII.GetString(br.ReadBytes(4));
                var dataSize = BinaryPrimitives.ReadInt32LittleEndian(br.ReadBytes(4));
                var typeFourCC = Encoding.ASCII.GetString(br.ReadBytes(4));
                if (fourCC != "RIFF" || typeFourCC != "PAL " || dataSize + 8 != stream.Length)
                    throw new InvalidRiffPalException();

                var paletteType = Encoding.ASCII.GetString(br.ReadBytes(4));
                if (paletteType != "data" && paletteType != "plth")
                    throw new UnsupportedRiffPaletteException(paletteType);
                if (paletteType == "plth")
                    throw new UnsupportedRiffPaletteException("Extended palette");

                var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(br.ReadBytes(4));
                if (chunkSize + 0x14 != stream.Length)
                    throw new InvalidRiffPalException();

                var minorVersion = br.ReadByte();
                var majorVersion = br.ReadByte();
                var colorCount = BinaryPrimitives.ReadInt32LittleEndian(br.ReadBytes(2));
                if (stream.Position + colorCount * 4 > stream.Length)
                    throw new InvalidRiffPalException();

                var palette = new List<Color>();
                for (int i = 0; i < colorCount; i++)
                {
                    var red = br.ReadByte();
                    var green = br.ReadByte();
                    var blue = br.ReadByte();
                    var flags = br.ReadByte();
                    palette.Add(Color.FromArgb(red, green, blue));
                }

                return new RiffPal(palette);
            }
        }

        public IList<Color> Palette { get; set; }

        private RiffPal(IList<Color> palette)
        {
            Palette = palette;
        }
    }
}
