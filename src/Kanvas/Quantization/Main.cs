using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using Kanvas.Format;
using Kanvas.Interface;
using Kanvas.Quantization.Helper;
using Kanvas.Quantization.Quantizer;
using Kanvas.Quantization.PathProvider;
using Kanvas.Quantization.ColorCache;

namespace Kanvas.Quantization
{
    public class Palette
    {
        public static (List<Color>, List<int>) CreatePalette(List<Color> colors, int Width, int Height, int colorCount, ColorQuantizer quantizer, Interface.PathProvider pathProvider, Interface.ColorCache colorCache)
        {
            unsafe
            {
                var rgbaData = new RGBA(8, 8, 8, 8, true).Save(colors);

                var img = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
                var imgData = (byte*)img.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb).Scan0;
                for (int i = 0; i < rgbaData.Length; i++) imgData[i] = rgbaData[i];

                var imgBuf = new ImageBuffer(img, ImageLockMode.ReadWrite);

                var q = GetQuantizer(quantizer);
                SetPathProvider(q, quantizer, GetPathProvider(pathProvider));
                SetColorCache(q, quantizer, GetColorCache(colorCache));

                var targetImg = (Bitmap)ImageBuffer.QuantizeImage(imgBuf, q, colorCount);

                var palette = q.GetPalette(colorCount);

                List<int> indeces = new List<int>();
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        indeces.Add(q.GetPaletteIndex(targetImg.GetPixel(x, y), 0, 0));

                return (palette, indeces);
            }
        }

        private static IColorQuantizer GetQuantizer(ColorQuantizer quantizer)
        {
            switch (quantizer)
            {
                case ColorQuantizer.DistinctSelection:
                    return new DistinctSelectionQuantizer();
                case ColorQuantizer.MediaCut:
                    return new MedianCutQuantizer();
                case ColorQuantizer.NeuralColor:
                    return new NeuralColorQuantizer();
                case ColorQuantizer.Octree:
                    return new OctreeQuantizer();
                case ColorQuantizer.OptimalPalette:
                    return new OptimalPaletteQuantizer();
                case ColorQuantizer.Popularity:
                    return new PopularityQuantizer();
                case ColorQuantizer.Uniform:
                    return new UniformQuantizer();
                case ColorQuantizer.WuColor:
                    return new WuColorQuantizer();
                default:
                    return null;
            }
        }

        private static IPathProvider GetPathProvider(Interface.PathProvider pathProvider)
        {
            switch (pathProvider)
            {
                case Interface.PathProvider.Reversed:
                    return new ReversedPathProvider();
                case Interface.PathProvider.Serpentive:
                    return new SerpentinePathProvider();
                case Interface.PathProvider.Standard:
                    return new StandardPathProvider();
                default:
                    return null;
            }
        }

        private static void SetPathProvider(IColorQuantizer quantizer, ColorQuantizer q, IPathProvider pathProvider)
        {
            switch (q)
            {
                case ColorQuantizer.DistinctSelection:
                    (quantizer as DistinctSelectionQuantizer).ChangePathProvider(pathProvider);
                    break;
                case ColorQuantizer.MediaCut:
                    (quantizer as MedianCutQuantizer).ChangePathProvider(pathProvider);
                    break;
                case ColorQuantizer.NeuralColor:
                    (quantizer as NeuralColorQuantizer).ChangePathProvider(pathProvider);
                    break;
                case ColorQuantizer.Octree:
                    (quantizer as OctreeQuantizer).ChangePathProvider(pathProvider);
                    break;
                case ColorQuantizer.OptimalPalette:
                    (quantizer as OptimalPaletteQuantizer).ChangePathProvider(pathProvider);
                    break;
                case ColorQuantizer.Popularity:
                    (quantizer as PopularityQuantizer).ChangePathProvider(pathProvider);
                    break;
                case ColorQuantizer.Uniform:
                    (quantizer as UniformQuantizer).ChangePathProvider(pathProvider);
                    break;
                case ColorQuantizer.WuColor:
                    (quantizer as WuColorQuantizer).ChangePathProvider(pathProvider);
                    break;
            }
        }

        private static IColorCache GetColorCache(Interface.ColorCache colorCache)
        {
            switch (colorCache)
            {
                case Interface.ColorCache.EuclideanDistance:
                    return new EuclideanDistanceColorCache();
                case Interface.ColorCache.LocalitySensitiveHash:
                    return new LshColorCache();
                case Interface.ColorCache.Octree:
                    return new OctreeColorCache();
                default:
                    return null;
            }
        }

        private static void SetColorCache(IColorQuantizer quantizer, ColorQuantizer q, IColorCache colorCache)
        {
            switch (q)
            {
                case ColorQuantizer.DistinctSelection:
                    (quantizer as DistinctSelectionQuantizer).ChangeCacheProvider(colorCache);
                    break;
                case ColorQuantizer.MediaCut:
                    (quantizer as MedianCutQuantizer).ChangeCacheProvider(colorCache);
                    break;
                case ColorQuantizer.OptimalPalette:
                    (quantizer as OptimalPaletteQuantizer).ChangeCacheProvider(colorCache);
                    break;
                case ColorQuantizer.Popularity:
                    (quantizer as PopularityQuantizer).ChangeCacheProvider(colorCache);
                    break;
                case ColorQuantizer.Uniform:
                case ColorQuantizer.WuColor:
                case ColorQuantizer.NeuralColor:
                case ColorQuantizer.Octree:
                    break;
            }
        }
    }
}
