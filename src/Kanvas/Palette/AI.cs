using Kanvas.Format;
using Kanvas.Interface;
using Kanvas.Models;
using Kanvas.Support;
using Komponent.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Palette
{
    public class AI : IPaletteImageFormat
    {
        public int AlphaDepth { get; }
        public int IndexDepth { get; }
        public string FormatName { get; }
        public ByteOrder ByteOrder { get; set; } = ByteOrder.LittleEndian;

        public AI(int alphaDepth, int indexDepth)
        {
            if (indexDepth <= 0) throw new ArgumentOutOfRangeException(nameof(indexDepth));
            if (alphaDepth <= 0) throw new ArgumentOutOfRangeException(nameof(alphaDepth));
            if ((alphaDepth + indexDepth) % 8 != 0) throw new InvalidOperationException("AlphaDepth + IndexDepth has to be dividable by 8.");

            AlphaDepth = alphaDepth;
            IndexDepth = indexDepth;
            FormatName = $"A{alphaDepth}I{indexDepth}";
        }

        public IEnumerable<IndexData> LoadIndeces(byte[] data)
        {
            var alphaShift = IndexDepth;

            using (var br = new BinaryReaderX(new MemoryStream(data), false, ByteOrder))
                while (br.BaseStream.Position < br.BaseStream.Length)
                    switch (IndexDepth)
                    {
                        case 8:
                            var b = br.ReadByte();
                            yield return new AlphaIndexData(Helper.ChangeBitDepth(b >> alphaShift, AlphaDepth, 8), IndexDepth);
                            break;
                        default:
                            throw new InvalidOperationException($"IndexDepth {IndexDepth} not supported.");
                    }
        }

        public Color RetrieveColor(IndexData indexData, IList<Color> palette)
        {
            var alphaIndexData = (AlphaIndexData)indexData;
            var color = palette[alphaIndexData.Index];
            return Color.FromArgb(alphaIndexData.Alpha, color.R, color.G, color.B);
        }
        
        public IndexData RetrieveIndex(Color color, IList<Color> palette)
        {
            var foundColor = palette.FirstOrDefault(c => c.R == color.R && c.G == color.G && c.B == color.B);
            if (foundColor == Color.Empty)
                throw new InvalidOperationException($"Color {color} was not found in palette.");
            return new AlphaIndexData(color.A, palette.IndexOf(foundColor));
        }

        public byte[] SaveIndices(IEnumerable<IndexData> indeces)
        {
            var alphaShift = IndexDepth;

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true, ByteOrder))
            {
                foreach (var indexData in indeces)
                {
                    var alphaIndexData = (AlphaIndexData)indexData;
                    switch (IndexDepth)
                    {
                        case 8:
                            var b = Helper.ChangeBitDepth(alphaIndexData.Alpha, 8, AlphaDepth) << alphaShift;
                            bw.Write((byte)(b | alphaIndexData.Index & ((1 << alphaIndexData.Index) - 1)));
                            break;
                        default:
                            throw new Exception($"IndexDepth {IndexDepth} not supported.");
                    }
                }
            }

            return ms.ToArray();
        }
    }

    internal class AlphaIndexData : IndexData
    {
        public int Alpha { get; }

        public AlphaIndexData(int alpha, int index) : base(index)
        {
            Alpha = alpha;
        }
    }
}
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
