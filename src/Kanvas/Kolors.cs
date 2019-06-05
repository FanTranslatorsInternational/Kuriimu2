using Kanvas.Interface;
using Kanvas.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Kanvas.IndexEncoding.Models;
using Kanvas.Quantization.Models.ColorCache;

namespace Kanvas
{
    // TODO: Add parallelization to image encodings
    /// <summary>
    /// Main processor for different <see cref="IColorEncoding"/>.
    /// </summary>
    public static class Kolors
    {
        #region General
        public static IList<Color> DecomposeImage(Bitmap image)
        {
            var result = new Color[image.Width * image.Height];

            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                var ptr = (int*)data.Scan0;
                for (int i = 0; i < image.Width * image.Height; i++)
                    result[i] = Color.FromArgb(ptr[i]);
            }
            image.UnlockBits(data);

            return result;
        }

        public static Bitmap ComposeImage(IList<Color> colors, int width, int height)
        {
            var image = new Bitmap(width, height);
            var data = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                var ptr = (int*)data.Scan0;
                for (int i = 0; i < image.Width * image.Height; i++)
                    ptr[i] = colors[i].ToArgb();
            }
            image.UnlockBits(data);

            return image;
        }
        #endregion

        #region Image encoding

        /// <summary>
        /// Loads the binary data with given settings as an image.
        /// </summary>
        /// <param name="bytes">Byte array containing the binary image data.</param>
        /// <param name="settings">The settings determining the final image output.</param>
        /// <returns>Loaded bitmap.</returns>
        public static Bitmap Load(byte[] bytes, ImageSettings settings)
        {
            int width = settings.Width, height = settings.Height;

            // Used only for the block compressions PVRTC and ASTC
            if (settings.Encoding is IColorEncodingKnownDimensions kd)
            {
                kd.Width = width;
                kd.Height = height;
            }

            var points = GetPointSequence(settings.Swizzle, settings.Width, settings.Height, settings.PadWidth, settings.PadHeight);

            var bmp = new Bitmap(width, height);
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                var imagePtr = (int*)data.Scan0;
                if (imagePtr == null) throw new ArgumentNullException(nameof(imagePtr));
                foreach (var (point, color) in points.Zip(settings.Encoding.Load(bytes), Tuple.Create))
                {
                    int x = point.X, y = point.Y;
                    if (0 > x || x >= width || 0 > y || y >= height) continue;

                    imagePtr[data.Stride * y / 4 + x] = settings.PixelShader?.Invoke(color).ToArgb() ?? color.ToArgb();
                }
            }
            bmp.UnlockBits(data);

            return bmp;
        }

        /// <summary>
        /// Converts a given bitmap, modified by given settings, in binary data.
        /// </summary>
        /// <param name="bmp">The bitmap, which will be converted.</param>
        /// <param name="settings">Settings like encoding, dimensions and swizzles.</param>
        /// <returns>Saved byte array.</returns>
        public static byte[] Save(Bitmap bmp, ImageSettings settings)
        {
            int width = settings.Width, height = settings.Height;

            if (settings.Encoding is IColorEncodingKnownDimensions ifkd)
            {
                ifkd.Width = width;
                ifkd.Height = height;
            }

            var colors = new List<Color>();
            var points = GetPointSequence(settings.Swizzle, settings.Width, settings.Height, settings.PadWidth, settings.PadHeight);

            foreach (var point in points)
            {
                int x = Clamp(point.X, 0, bmp.Width);
                int y = Clamp(point.Y, 0, bmp.Height);

                var color = bmp.GetPixel(x, y);

                if (settings.PixelShader != null) color = settings.PixelShader(color);

                colors.Add(color);
            }

            return settings.Encoding.Save(colors);
        }

        #endregion

        #region Index based methods

        /// <summary>
        /// Loads the binary data with given settings as an image.
        /// </summary>
        /// <param name="bytes">Byte array containing the binary image data.</param>
        /// <param name="paletteBytes">Byte array containing the binary palette data.</param>
        /// <param name="settings">The settings determining the final image output.</param>
        /// <returns>Loaded bitmap, indices and colors.</returns>
        public static (Bitmap image, IList<Color> palette) Load(byte[] bytes, byte[] paletteBytes, IndexedImageSettings settings)
        {
            int width = settings.Width, height = settings.Height;

            var points = GetPointSequence(settings.Swizzle, settings.Width, settings.Height, settings.PadWidth, settings.PadHeight);
            var indices = settings.IndexEncoding.Load(bytes);
            var palette = settings.Encoding.Load(paletteBytes).ToList();

            var bmp = new Bitmap(width, height);
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                var imagePtr = (int*)data.Scan0;
                if (imagePtr == null)
                    throw new ArgumentNullException(nameof(imagePtr));

                var colors = settings.IndexEncoding.Compose(indices, palette);
                foreach (var (point, color) in points.Zip(colors, Tuple.Create))
                {
                    int x = point.X, y = point.Y;
                    if (0 > x || x >= width || 0 > y || y >= height)
                        continue;

                    imagePtr[data.Stride * y / 4 + x] = color.ToArgb();
                }
            }
            bmp.UnlockBits(data);

            return (bmp, palette);
        }

        /// <summary>
        /// Converts a given bitmap, modified by given settings, in binary data.
        /// </summary>
        /// <param name="bmp">The bitmap, which will be converted.</param>
        /// <param name="palette">The list containing all colors of the palette to use.</param>
        /// <param name="settings">The settings determining the final binary data output.</param>
        /// <returns><see cref="Tuple"/> containing 2 byte arrays.</returns>
        public static (byte[] indexData, byte[] paletteData) Save(Bitmap bmp, IndexedImageSettings settings)
        {
            // Create swizzle point list
            var points = GetPointSequence(settings.Swizzle, settings.Width, settings.Height, settings.PadWidth, settings.PadHeight);

            // Get swizzled collection of colors
            var colors = new List<Color>();
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                var imagePtr = (int*)data.Scan0;
                if (imagePtr == null)
                    throw new ArgumentNullException(nameof(imagePtr));

                foreach (var point in points)
                {
                    var x = Clamp(point.X, 0, bmp.Width);
                    var y = Clamp(point.Y, 0, bmp.Height);

                    colors.Add(Color.FromArgb(imagePtr[y * bmp.Width + x]));
                }
            }
            bmp.UnlockBits(data);

            // Decompose/Quantize colors
            var (indices, palette) = settings.QuantizationSettings != null ?
                settings.IndexEncoding.Quantize(colors, settings.QuantizationSettings) :
                settings.IndexEncoding.Decompose(colors);

            return (settings.IndexEncoding.Save(indices), settings.Encoding.Save(palette));
        }

        #endregion

        #region Image quantization

        /// <summary>
        /// Quantizes an image.
        /// </summary>
        /// <param name="image">Image to quantize.</param>
        /// <param name="settings">Settings for quantization processes.</param>
        /// <returns>Collection of indices and a palette.</returns>
        public static (IEnumerable<int> indeces, IList<Color> palette) Quantize(Bitmap image,
            QuantizationSettings settings)
            => Quantize(DecomposeImage(image), settings);

        /// <summary>
        /// Quantizes a collection of colors.
        /// </summary>
        /// <param name="colors">Collection of colors to quantize.</param>
        /// <param name="settings">Settings for quantization processes.</param>
        /// <returns>Collection of indices and a palette.</returns>
        public static (IEnumerable<int> indeces, IList<Color> palette) Quantize(IEnumerable<Color> colors, QuantizationSettings settings)
        {
            SetupQuantization(settings);

            var indices = settings.Ditherer?.Process(colors) ?? settings.Quantizer.Process(colors);
            return (indices, settings.Quantizer.GetPalette());
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Clamps a value.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum to clamp to.</param>
        /// <param name="max">Maximum to clamp to.</param>
        /// <returns></returns>
        private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max - 1);

        /// <summary>
        /// Gives back a sequence of points, modified by swizzles if applied
        /// </summary>
        private static IEnumerable<Point> GetPointSequence(IImageSwizzle swizzle, int width, int height, int padWidth, int padHeight)
        {
            int strideWidth = swizzle?.Width ?? (padWidth <= 0 ? width : padWidth);
            int strideHeight = swizzle?.Height ?? (padHeight <= 0 ? height : padHeight);

            for (var i = 0; i < strideWidth * strideHeight; i++)
            {
                var point = new Point(i % strideWidth, i / strideWidth);
                if (swizzle != null)
                    point = swizzle.Get(point);

                yield return point;
            }
        }

        private static void SetupQuantization(QuantizationSettings settings)
        {
            // Check arguments
            if (settings.Quantizer.UsesColorCache && settings.ColorCache == null)
                throw new ArgumentNullException(nameof(settings.ColorCache));
            if (settings.Quantizer.AllowParallel && settings.ParallelCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(settings.ParallelCount));
            if (settings.Quantizer.UsesVariableColorCount && settings.ColorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(settings.ColorCount));

            // Prepare objects
            settings.Ditherer?.Prepare(settings.Quantizer, settings.Width, settings.Height);
            settings.ColorCache?.Prepare(settings.ColorModel, settings.ColorModel == ColorModel.RGBA ? settings.AlphaThreshold : 0);

            if (settings.Quantizer.UsesColorCache)
                settings.Quantizer.SetColorCache(settings.ColorCache);
            if (settings.Quantizer.UsesVariableColorCount)
                settings.Quantizer.SetColorCount(settings.ColorCount);
            if (settings.Quantizer.AllowParallel)
                settings.Quantizer.SetParallelTasks(settings.ParallelCount);
        }

        #endregion
    }
}