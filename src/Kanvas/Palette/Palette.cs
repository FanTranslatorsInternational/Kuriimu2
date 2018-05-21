using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Interface;
using System.Drawing;
using Komponent.IO;
using System.IO;
using Kanvas.Support;

namespace Kanvas.Palette
{
    public class Palette : IPaletteFormat
    {
        List<Color> colors;
        List<Color> savedColors;

        public int BitDepth { get; }

        public string FormatName { get; private set; }

        int indexDepth;
        ByteOrder byteOrder;

        public Palette(int indexDepth = 8, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            if (indexDepth % 4 != 0) throw new Exception("IndexDepth has to be dividable by 4.");

            this.byteOrder = byteOrder;

            BitDepth = indexDepth;
            FormatName = "Palette";

            /*this.paletteFormat = paletteFormat;
            paletteBytes = paletteData;
            colors = paletteFormat.Load(paletteData).ToList();*/
        }

        public void SetPalette(byte[] paletteData, IImageFormat paletteFormat)
        {
            FormatName = "Paletted " + paletteFormat.FormatName;
            colors = paletteFormat.Load(paletteData).ToList();
        }
        public void SetPalette(IEnumerable<Color> paletteColors)
        {
            colors = paletteColors.ToList();
        }

        public IEnumerable<Color> Load(byte[] data)
        {
            using (var br = new BinaryReaderX(new MemoryStream(data), true, byteOrder))
                while (true)
                    switch (BitDepth)
                    {
                        case 4:
                            yield return colors[br.ReadNibble()];
                            break;
                        case 8:
                            yield return colors[br.ReadByte()];
                            break;
                        default:
                            throw new Exception($"BitDepth {BitDepth} not supported!");
                    }
        }

        public byte[] Save(IEnumerable<Color> colors, ColorDistance colorDistance = ColorDistance.DirectDistance)
        {
            savedColors = CreatePalette(colors.ToList(), colorDistance);

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true, byteOrder))
            {
                foreach (var color in colors)
                    switch (BitDepth)
                    {
                        case 4:
                            bw.WriteNibble(savedColors.FindIndex(c => c == color));
                            break;
                        case 8:
                            bw.Write((byte)savedColors.FindIndex(c => c == color));
                            break;
                        default:
                            throw new Exception($"BitDepth {BitDepth} not supported!");
                    }
            }

            return ms.ToArray();
        }

        public byte[] GetPalette(IImageFormat paletteFormat)
        {
            return paletteFormat.Save(savedColors);
        }
        public IEnumerable<Color> GetPalette()
        {
            return savedColors;
        }

        private List<Color> CreatePalette(List<Color> colors, ColorDistance colorDistance)
        {
            List<Color> reducedColors = new List<Color>();
            foreach (var color in colors)
                if (reducedColors.Count >= (1 << indexDepth) - 1)
                {
                    //get color by color weithing
                    reducedColors.Add(Helper.GetClosesColor(colors, color, colorDistance));
                }
                else
                {
                    //add unknown color
                    if (!reducedColors.Exists(c => c == color)) reducedColors.Add(color);
                }

            return reducedColors;
        }
    }
}
