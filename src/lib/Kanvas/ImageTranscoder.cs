using Kanvas.Contract;
using Kanvas.Contract.Configuration;
using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using Kanvas.Contract.Quantization;
using Kanvas.DataClasses;
using Kanvas.DataClasses.Configuration;
using Kanvas.Quantization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas
{
    /// <summary>
    /// The class to implement transcoding actions on data and images.
    /// </summary>
    internal class ImageTranscoder(ImageTranscoderOptions options) : IImageTranscoder
    {
        #region Decode methods

        public Image<Rgba32> Decode(byte[] data, Size imageSize)
        {
            return DecodeColor(data, imageSize);
        }

        public Image<Rgba32> Decode(byte[] data, byte[] paletteData, Size imageSize)
        {
            return DecodeIndex(data, paletteData, imageSize);
        }

        private Image<Rgba32> DecodeColor(byte[] data, Size imageSize)
        {
            IColorEncoding? colorEncoding = options.EncodingOptions.ColorEncoding;
            ArgumentNullException.ThrowIfNull(colorEncoding);

            // Prepare information and instances
            Size paddedSize = GetPaddedSize(imageSize);
            IImageSwizzle? swizzle = GetPixelRemapper(colorEncoding, paddedSize);
            Size finalSize = GetFinalSize(paddedSize, swizzle);

            // Load colors
            int expectedColorCount = finalSize.Width * finalSize.Height;
            int expectedDataLength = expectedColorCount / colorEncoding.ColorsPerValue * colorEncoding.BitsPerValue / 8;

            // HINT: If the given data is shorter than what is needed for the full image, we throw.
            //       Otherwise enough data is given and the image can be fully decoded, even if excess data is not used.
            if (data.Length < expectedDataLength)
                throw new InvalidOperationException($"Given data is too short (Given: {data.Length} Bytes, Expected: {expectedDataLength} Bytes).");

            var options1 = new EncodingOptions
            {
                TaskCount = options.ImageOptions.TaskCount,
                Size = finalSize
            };
            IEnumerable<Rgba32> colors = colorEncoding.Load(data, options1);

            // Apply color shader
            CreateColorShaderDelegate? shaderDelegate = options.ColorShaderOptions.ColorShaderDelegate;
            if (shaderDelegate != null)
            {
                IColorShader shader = shaderDelegate();
                colors = colors.Select(shader.Read);
            }

            // Create image with unpadded dimensions
            return colors.ToImage(imageSize, paddedSize, swizzle, options.ImageOptions.Anchor);
        }

        private Image<Rgba32> DecodeIndex(byte[] data, byte[] paletteData, Size imageSize)
        {
            IIndexEncoding? indexEncoding = options.EncodingOptions.IndexEncoding;
            IColorEncoding? paletteEncoding = options.EncodingOptions.PaletteEncoding;

            ArgumentNullException.ThrowIfNull(indexEncoding);
            ArgumentNullException.ThrowIfNull(paletteEncoding);

            // Prepare information and instances
            Size paddedSize = GetPaddedSize(imageSize);
            IImageSwizzle? swizzle = GetPixelRemapper(indexEncoding, paddedSize);
            Size finalSize = GetFinalSize(paddedSize, swizzle);

            // Load palette
            int paletteColorCount = paletteData.Length * 8 / paletteEncoding.BitsPerValue * paletteEncoding.ColorsPerValue;

            var options1 = new EncodingOptions
            {
                TaskCount = options.ImageOptions.TaskCount,
                Size = new Size(1, paletteColorCount)
            };
            IEnumerable<Rgba32> paletteEnumeration = paletteEncoding.Load(paletteData, options1);

            // Apply color shader on palette
            CreateColorShaderDelegate? shaderDelegate = options.ColorShaderOptions.ColorShaderDelegate;
            if (shaderDelegate != null)
            {
                IColorShader shader = shaderDelegate();
                paletteEnumeration = paletteEnumeration.Select(shader.Read);
            }

            Rgba32[] palette = [.. paletteEnumeration];

            // Load indices
            options1 = new EncodingOptions
            {
                TaskCount = options.ImageOptions.TaskCount,
                Size = finalSize
            };
            IEnumerable<Rgba32> colors = indexEncoding.Load(data, palette, options1);

            return colors.ToImage(imageSize, paddedSize, swizzle, options.ImageOptions.Anchor);
        }

        #endregion

        #region Encode methods

        public (byte[] imageData, byte[]? paletteData) Encode(Image<Rgba32> image)
        {
            if (options.EncodingOptions is { IndexEncoding: not null, PaletteEncoding: not null })
                return EncodeIndex(image);

            return (EncodeColor(image), null);
        }

        private byte[] EncodeColor(Image<Rgba32> image)
        {
            IColorEncoding? colorEncoding = options.EncodingOptions.ColorEncoding;
            ArgumentNullException.ThrowIfNull(colorEncoding);

            // Prepare information and instances
            Size paddedSize = GetPaddedSize(image.Size);
            IImageSwizzle? swizzle = GetPixelRemapper(colorEncoding, paddedSize);
            Size finalSize = GetFinalSize(paddedSize, swizzle);

            IEnumerable<Rgba32> colors;
            if (options.QuantizationOptions != null)
            {
                if (options.QuantizationOptions.ColorCount < 0)
                    options.QuantizationOptions.ColorCount = 256;

                options.QuantizationOptions.ColorChannelBitDepths = GetPaletteQuantizationBitDepths();

                // If we have quantization enabled
                IQuantizer quantizer = new Quantizer(options.QuantizationOptions);

                // HINT: Color shader is applied by QuantizeImage
                (IEnumerable<int> indices, IList<Rgba32> palette) = QuantizeImage(image, finalSize, quantizer, swizzle);

                // Recompose indices to colors
                colors = indices.ToColors(palette);
            }
            else
            {
                // Decompose image to colors
                colors = image.ToColors(paddedSize, swizzle, options.ImageOptions.Anchor);

                // Apply color shader
                CreateColorShaderDelegate? shaderDelegate = options.ColorShaderOptions.ColorShaderDelegate;
                if (shaderDelegate != null)
                {
                    IColorShader shader = shaderDelegate();
                    colors = colors.Select(shader.Write);
                }
            }

            // Save color data
            var options1 = new EncodingOptions
            {
                TaskCount = options.ImageOptions.TaskCount,
                Size = finalSize
            };
            return colorEncoding.Save(colors, options1);
        }

        private (byte[], byte[]) EncodeIndex(Image<Rgba32> image)
        {
            IIndexEncoding? indexEncoding = options.EncodingOptions.IndexEncoding;
            IColorEncoding? paletteEncoding = options.EncodingOptions.PaletteEncoding;
            QuantizationConfigurationOptions? quantizationOptions = options.QuantizationOptions;

            ArgumentNullException.ThrowIfNull(indexEncoding);
            ArgumentNullException.ThrowIfNull(paletteEncoding);
            ArgumentNullException.ThrowIfNull(quantizationOptions);

            // Prepare information and instances
            Size paddedSize = GetPaddedSize(image.Size);
            IImageSwizzle? swizzle = GetPixelRemapper(indexEncoding, paddedSize);
            Size finalSize = GetFinalSize(paddedSize, swizzle);

            quantizationOptions.ColorCount = quantizationOptions.ColorCount < 0
                ? indexEncoding.MaxColors
                : Math.Min(quantizationOptions.ColorCount, indexEncoding.MaxColors);
            quantizationOptions.ColorChannelBitDepths = GetPaletteQuantizationBitDepths();

            IQuantizer quantizer = new Quantizer(quantizationOptions);
            (IEnumerable<int> indices, IList<Rgba32> palette) = QuantizeImage(image, finalSize, quantizer, swizzle);

            // Save palette colors
            // This step can be skipped if no palette encoding is given.
            //   That saves time in the scenario when the palette is not needed or already exists as encoded data from somewhere else.
            var options1 = new EncodingOptions
            {
                TaskCount = options.ImageOptions.TaskCount,
                Size = new Size(1, palette.Count)
            };
            byte[] paletteData = paletteEncoding.Save(palette, options1);

            // Save image indexes
            options1 = new EncodingOptions
            {
                TaskCount = options.ImageOptions.TaskCount,
                Size = finalSize
            };
            byte[] indexData = indexEncoding.Save(indices, palette, options1);

            return (indexData, paletteData);
        }

        #endregion

        private (IEnumerable<int> indices, IList<Rgba32> palette) QuantizeImage(Image<Rgba32> image, Size finalSize,
            IQuantizer quantizer, IImageSwizzle? swizzle)
        {
            // Decompose unswizzled image to colors
            IEnumerable<Rgba32> colors = image.ToColors(finalSize);

            // Quantize unswizzled indices
            (IEnumerable<int> indices, IList<Rgba32> palette) = quantizer.Process(colors, finalSize);

            // Apply color shader
            CreateColorShaderDelegate? shaderDelegate = options.ColorShaderOptions.ColorShaderDelegate;
            if (shaderDelegate != null)
            {
                IColorShader shader = shaderDelegate();
                palette = [.. palette.Select(shader.Write)];
            }

            // Delegate indices to correct positions
            IEnumerable<int> swizzledIndices = SwizzleIndices([.. indices], finalSize, swizzle);

            return (swizzledIndices, palette);
        }

        private static IEnumerable<int> SwizzleIndices(IList<int> indices, Size imageSize, IImageSwizzle? swizzle)
        {
            return Composition.GetPointSequence(imageSize, swizzle)
                .Select(point => indices[GetIndex(point, imageSize)]);
        }

        private static int GetIndex(Point point, Size imageSize)
        {
            return point.Y * imageSize.Width + point.X;
        }

        private Size GetPaddedSize(Size imageSize)
        {
            int width = options.SizePaddingOptions.WidthDelegate?.Invoke(imageSize.Width) ?? imageSize.Width;
            int height = options.SizePaddingOptions.HeightDelegate?.Invoke(imageSize.Height) ?? imageSize.Height;

            return new Size(width, height);
        }

        private IImageSwizzle? GetPixelRemapper(IEncodingInfo encodingInfo, Size paddedSize)
        {
            if (options.PixelRemappingOptions.PixelRemappingDelegate == null)
                return null;

            var options1 = new SwizzleOptions
            {
                EncodingInfo = encodingInfo,
                Size = paddedSize
            };
            return options.PixelRemappingOptions.PixelRemappingDelegate(options1);
        }

        private static Size GetFinalSize(Size paddedSize, IImageSwizzle? swizzle)
        {
            // Swizzle dimensions are based on padded size already
            // Swizzle has higher priority since it might pad the padded size further, due to its macro blocks
            if (swizzle != null)
                return new Size(swizzle.Width, swizzle.Height);

            // Otherwise just return the already padded size
            return paddedSize;
        }

        private ColorChannelBitDepths GetPaletteQuantizationBitDepths()
        {
            IColorEncoding? paletteEncoding = options.EncodingOptions.PaletteEncoding;
            return paletteEncoding?.ColorChannelBitDepths ?? ColorChannelBitDepths.Unknown;
        }
    }
}
