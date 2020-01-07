using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontract;
using Kontract.Kanvas;
using Kontract.Kanvas.Configuration;

namespace Kanvas.Configuration
{
    class Transcoder : IColorTranscoder, IIndexTranscoder
    {
        private Size _imageSize;
        private Size _paddedSize;

        private IImageSwizzle _swizzle;

        private IColorIndexEncoding _indexEncoding;
        private IColorEncoding _paletteEncoding;
        private IQuantizationConfiguration _quantizationConfiguration;

        private IColorEncoding _colorEncoding;

        public Transcoder(Size size, Size paddedSize, IColorIndexEncoding indexEncoding, IColorEncoding paletteEncoding,
            IQuantizationConfiguration quantizationConfig, IImageSwizzle swizzle)
        {
            ContractAssertions.IsNotNull(indexEncoding, nameof(indexEncoding));
            ContractAssertions.IsNotNull(paletteEncoding, nameof(paletteEncoding));
            ContractAssertions.IsNotNull(quantizationConfig, nameof(quantizationConfig));

            _imageSize = size;
            _paddedSize = paddedSize;

            _indexEncoding = indexEncoding;
            _paletteEncoding = paletteEncoding;
            _quantizationConfiguration = quantizationConfig;
            _swizzle = swizzle;
        }

        public Transcoder(Size size, Size paddedSize, IColorEncoding colorEncoding, IImageSwizzle swizzle)
        {
            ContractAssertions.IsNotNull(colorEncoding, nameof(colorEncoding));

            _imageSize = size;
            _paddedSize = paddedSize;

            _colorEncoding = colorEncoding;
            _swizzle = swizzle;
        }

        public Image Decode(byte[] data)
        {
            // Load indexColors
            var colors = _colorEncoding.Load(data);

            // Compose image
            return Composition.ComposeImage(colors, _imageSize, _paddedSize, _swizzle);
        }

        public Image Decode(byte[] data, byte[] paletteData)
        {
            // Load palette indexColors
            var palette = _paletteEncoding.Load(paletteData).ToList();

            // Load image indexColors
            var indexColors = _indexEncoding.Load(data);
            var colors = GetColorsFromIndices(indexColors, palette, _indexEncoding);

            // Compose image
            return Composition.ComposeImage(colors, _imageSize, _paddedSize, _swizzle);
        }

        byte[] IColorTranscoder.Encode(Bitmap image)
        {
            // Decompose image to indexColors
            var colors = Composition.DecomposeImage(image, _paddedSize, _swizzle);

            // Save color data
            return _colorEncoding.Save(colors);
        }

        // ReSharper disable PossibleMultipleEnumeration
        (byte[] indexData, byte[] paletteData) IIndexTranscoder.Encode(Bitmap image)
        {
            // Decompose unswizzled image to indexColors
            var colors = Composition.DecomposeImage(image, _paddedSize);

            // Quantize unswizzled indexColors
            var quantizer = _quantizationConfiguration.WithImageSize(image.Size).Build();
            var (indices, palette) = quantizer.Process(colors);

            // Swizzle indexColors to correct positions
            var indexColors = SwizzleIndices(Zip(indices, colors), image.Size, _paddedSize, _swizzle);

            // Save palette indexColors
            var paletteData = _paletteEncoding.Save(palette);

            // Save image indexColors
            var indexData = _indexEncoding.Save(indexColors);

            return (indexData, paletteData);
        }

        private IEnumerable<Color> GetColorsFromIndices(IEnumerable<(int, Color)> indexColors, IList<Color> palette, IColorIndexEncoding indexEncoding)
        {
            return indexColors.Select(x => indexEncoding.GetColorFromIndex(x, palette));
        }

        private IEnumerable<(int, Color)> SwizzleIndices(IEnumerable<(int, Color)> indexColors, Size imageSize, Size paddedSize, IImageSwizzle swizzle)
        {
            var indexColorPoints = Zip(indexColors, Composition.GetPointSequence(imageSize, paddedSize, swizzle));
            return indexColorPoints.OrderBy(cp => GetIndex(cp.Second, imageSize)).Select(x => x.First);
        }

        private int GetIndex(Point point, Size imageSize)
        {
            return point.Y * imageSize.Width + point.X;
        }

        // TODO: Remove when targeting only netcoreapp31
        private IEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
#if NET_CORE_31
            return first.Zip(second);
#else
            return first.Zip(second, (f, s) => (f, s));
#endif
        }
    }
}
