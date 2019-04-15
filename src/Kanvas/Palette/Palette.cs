//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Kanvas.Interface;
//using System.Drawing;
//using Komponent.IO;
//using System.IO;
//using Kanvas.Support;

//namespace Kanvas.Palette
//{
//    public class Palette : IPaletteFormat
//    {
//        List<Color> colors;
//        List<Color> savedColors;

//        public int BitDepth { get; }

//        public string FormatName { get; private set; }

//        public bool IsBlockCompression { get => false; }

//        public ColorQuantizer ColorQuantizer { get; set; }
//        public PathProvider PathProvider { get; set; }
//        public ColorCache ColorCache { get; set; }

//        public int Width { get; set; }
//        public int Height { get; set; }

//        int indexDepth;
//        int colorCount;
//        ByteOrder byteOrder;

//        public Palette(int indexDepth = 8, int colorCount = -1, ByteOrder byteOrder = ByteOrder.LittleEndian)
//        {
//            if (indexDepth % 4 != 0) throw new Exception("IndexDepth has to be dividable by 4.");

//            this.byteOrder = byteOrder;

//            this.indexDepth = indexDepth;
//            this.colorCount = colorCount;
//            BitDepth = indexDepth;
//            FormatName = "Palette " + indexDepth + "Bit";

//            ColorQuantizer = ColorQuantizer.DistinctSelection;
//            PathProvider = PathProvider.Standard;
//            ColorCache = ColorCache.EuclideanDistance;

//            Width = -1;
//            Height = -1;
//        }

//        public void SetPalette(byte[] paletteData, IImageFormat paletteFormat)
//        {
//            FormatName = "Paletted " + paletteFormat.FormatName;
//            colors = paletteFormat.Load(paletteData).ToList();
//        }
//        public void SetPalette(IEnumerable<Color> paletteColors)
//        {
//            colors = paletteColors.ToList();
//        }

//        public IEnumerable<Color> Load(byte[] data)
//        {
//            using (var br = new BinaryReaderX(new MemoryStream(data), true, byteOrder))
//                while (br.BaseStream.Position < br.BaseStream.Length || !br.IsFirstNibble)
//                    switch (BitDepth)
//                    {
//                        case 4:
//                            yield return colors[br.ReadNibble()];
//                            break;
//                        case 8:
//                            yield return colors[br.ReadByte()];
//                            break;
//                        default:
//                            throw new Exception($"BitDepth {BitDepth} not supported!");
//                    }
//        }

//        public byte[] Save(IEnumerable<Color> colors)
//        {
//            if (Width <= 0 || Height <= 0)
//                throw new Exception("You need to set Width and Height for saving palette textures.");

//            var (palette, indeces) = Quantization.Palette.CreatePalette(colors.ToList(), Width, Height, (colorCount == -1) ? 1 << indexDepth : colorCount, ColorQuantizer, PathProvider, ColorCache);
//            savedColors = palette;
//            this.colors = palette;

//            var ms = new MemoryStream();
//            using (var bw = new BinaryWriterX(ms, true, byteOrder))
//            {
//                for (int i = 0; i < colors.Count(); i++)
//                    switch (BitDepth)
//                    {
//                        case 4:
//                            bw.WriteNibble(indeces[i]);
//                            break;
//                        case 8:
//                            bw.Write((byte)indeces[i]);
//                            break;
//                        default:
//                            throw new Exception($"BitDepth {BitDepth} not supported!");
//                    }
//            }

//            return ms.ToArray();
//        }

//        public byte[] GetPalette(IImageFormat paletteFormat)
//        {
//            return paletteFormat.Save(savedColors);
//        }
//        public IEnumerable<Color> GetPalette()
//        {
//            return savedColors;
//        }
//    }
//}
