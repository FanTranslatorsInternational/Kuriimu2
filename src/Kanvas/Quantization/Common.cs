using Kanvas.Quantization.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kanvas.Quantization.ColorCaches;
using Kanvas.Quantization.Models.ColorCache;

namespace Kanvas.Quantization
{
    public class QuantizeImageSettings
    {
        public IColorDitherer Ditherer { get; set; }
        public IColorQuantizer Quantizer { get; }
        public IColorCache ColorCache { get; set; }
        public ColorModel ColorModel { get; set; }
        public int ColorCount { get; set; }
        public int ParallelCount { get; set; }

        public QuantizeImageSettings(IColorQuantizer quantizer)
        {
            Quantizer = quantizer ?? throw new ArgumentNullException(nameof(quantizer));

            Ditherer = null;
            ColorCache = new EuclideanDistanceColorCache();
            ColorModel = ColorModel.RGB;
            ColorCount = 256;
            ParallelCount = 8;
        }
    }

    public class Common
    {
        public static (IEnumerable<int> indeces, IList<Color> palette) Quantize(Bitmap image, QuantizeImageSettings settings)
        {
            Setup(image, settings);

            var colors = Decompose(image);

            var indices = settings.Ditherer?.Process(colors) ?? settings.Quantizer.Process(colors);
            return (indices, settings.Quantizer.GetPalette());
        }

        private static void Setup(Bitmap image, QuantizeImageSettings settings)
        {
            // Check arguments
            if (settings.Quantizer.UsesColorCache && settings.ColorCache == null)
                throw new ArgumentNullException(nameof(settings.ColorCache));
            if (settings.Quantizer.AllowParallel && settings.ParallelCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(settings.ParallelCount));
            if (settings.Quantizer.UsesVariableColorCount && settings.ColorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(settings.ColorCount));

            // Prepare objects
            settings.Ditherer?.Prepare(settings.Quantizer, image.Width, image.Height);
            settings.ColorCache?.Prepare(settings.ColorModel);

            if (settings.Quantizer.UsesColorCache)
                settings.Quantizer.SetColorCache(settings.ColorCache);
            if (settings.Quantizer.UsesVariableColorCount)
                settings.Quantizer.SetColorCount(settings.ColorCount);
            if (settings.Quantizer.AllowParallel)
                settings.Quantizer.SetParallelTasks(settings.ParallelCount);
        }

        public static IList<Color> Decompose(Bitmap image)
        {
            var result = new Color[image.Width * image.Height];

            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            unsafe
            {
                var ptr = (int*)data.Scan0;
                for (int i = 0; i < image.Width * image.Height; i++)
                    result[i] = Color.FromArgb(ptr[i]);
            }
            image.UnlockBits(data);

            return result;
        }

        public static Bitmap Compose(IList<int> indices, IList<Color> palette, int width, int height)
        {
            var image = new Bitmap(width, height);

            var data = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            unsafe
            {
                var ptr = (int*)data.Scan0;
                for (int i = 0; i < width * height; i++)
                    ptr[i] = palette[indices[i]].ToArgb();
            }
            image.UnlockBits(data);

            return image;
        }
    }
}
