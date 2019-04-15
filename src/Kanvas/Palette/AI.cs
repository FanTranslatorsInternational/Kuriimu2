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
//using Kanvas.Format;

//namespace Kanvas.Palette
//{
//    public class AI : IPaletteFormat
//    {
//        List<Color> colors = null;
//        List<Color> savedColors;

//        public int BitDepth { get; }
//        public string FormatName { get; }

//        public bool IsBlockCompression { get => false; }

//        public ColorQuantizer ColorQuantizer { get; set; }
//        public PathProvider PathProvider { get; set; }
//        public ColorCache ColorCache { get; set; }

//        public int Width { get; set; }
//        public int Height { get; set; }

//        int alphaDepth;
//        int indexDepth;

//        ByteOrder byteOrder;

//        public AI(int alphaDepth, int indexDepth, ByteOrder byteOrder = ByteOrder.LittleEndian)
//        {
//            if ((alphaDepth + indexDepth) % 8 != 0) throw new Exception("Alpha + IndexSize has to be dividable by 8.");
//            if (indexDepth <= 0) throw new Exception($"IndexDepth is 0 but Alpha is not. You might want to use the LA format with L=0 and A={alphaDepth}");

//            this.alphaDepth = alphaDepth;
//            this.indexDepth = indexDepth;

//            this.byteOrder = byteOrder;

//            BitDepth = alphaDepth + indexDepth;
//            FormatName = $"A{alphaDepth}I{indexDepth}";

//            ColorQuantizer = ColorQuantizer.DistinctSelection;
//            PathProvider = PathProvider.Serpentive;
//            ColorCache = ColorCache.LocalitySensitiveHash;

//            Width = -1;
//            Height = -1;
//        }

//        public void SetPalette(byte[] paletteData, IImageFormat paletteFormat)
//        {
//            colors = paletteFormat.Load(paletteData).ToList();
//        }
//        public void SetPalette(IEnumerable<Color> paletteColors)
//        {
//            colors = paletteColors.ToList();
//        }

//        public IEnumerable<Color> Load(byte[] data)
//        {
//            if (colors == null)
//                throw new Exception("You have to set a palette first.");

//            var alphaShift = indexDepth;

//            using (var br = new BinaryReaderX(new MemoryStream(data), true, byteOrder))
//                while (true)
//                    switch (BitDepth)
//                    {
//                        case 8:
//                            var b = br.ReadByte();
//                            yield return Color.FromArgb(
//                                alphaShift == 8 ? 255 : Helper.ChangeBitDepth(b >> alphaShift, alphaDepth, 8),
//                                colors[b & ((1 << indexDepth) - 1)]);
//                            break;
//                        default:
//                            throw new Exception($"BitDepth {BitDepth} not supported!");
//                    }
//        }

//        /// <summary>
//        /// Converts an Enumerable of colors into a byte[].
//        /// Palette will be written before the image data
//        /// </summary>
//        /// <param name="colors"></param>
//        /// <returns></returns>
//        public byte[] Save(IEnumerable<Color> colors)
//        {
//            if (Width <= 0 || Height <= 0)
//                throw new Exception("You need to set Width and Height for saving palette textures.");

//            var (palette, indeces) = Quantization.Palette.CreatePalette(colors.ToList(), Width, Height, 1 << indexDepth, ColorQuantizer, PathProvider, ColorCache);
//            savedColors = palette;
//            this.colors = palette;

//            var alphaShift = indexDepth;

//            var listColor = colors.ToList();
//            var ms = new MemoryStream();
//            using (var bw = new BinaryWriterX(ms, true, byteOrder))
//            {
//                for (int i = 0; i < colors.Count(); i++)
//                    switch (BitDepth)
//                    {
//                        case 8:
//                            byte b = (byte)(Helper.ChangeBitDepth(listColor[i].A, 8, alphaDepth) << alphaShift);
//                            bw.Write((byte)(b | indeces[i]));
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
