using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontract;
using Kontract.Kanvas;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    class Transcoder : IColorTranscoder, IIndexTranscoder
    {
        private Size _imageSize;
        private Size _paddedSize;

        private IImageSwizzle _swizzle;

        private IColorIndexEncoding _indexEncoding;
        private IColorEncoding _paletteEncoding;
        private IQuantizer _quantizer;

        private IColorEncoding _colorEncoding;

        public Transcoder(Size size, Size paddedSize, IColorIndexEncoding indexEncoding, IColorEncoding paletteEncoding,
            IQuantizer quantizer, IImageSwizzle swizzle)
        {
            ContractAssertions.IsNotNull(indexEncoding, nameof(indexEncoding));
            ContractAssertions.IsNotNull(paletteEncoding, nameof(paletteEncoding));
            ContractAssertions.IsNotNull(quantizer, nameof(quantizer));

            _imageSize = size;
            _paddedSize = paddedSize;

            _indexEncoding = indexEncoding;
            _paletteEncoding = paletteEncoding;
            _quantizer = quantizer;
            _swizzle = swizzle;
        }

        public Transcoder(Size size, Size paddedSize, IColorEncoding colorEncoding,
            IQuantizer quantizer, IImageSwizzle swizzle)
        {
            ContractAssertions.IsNotNull(colorEncoding, nameof(colorEncoding));

            _imageSize = size;
            _paddedSize = paddedSize;

            _colorEncoding = colorEncoding;
            _quantizer = quantizer;
            _swizzle = swizzle;
        }

        public Image Decode(byte[] data)
        {
            // Load indexColors
            var colors = _colorEncoding.Load(data);

            // Compose image
            var imageSize = _paddedSize == Size.Empty ? _imageSize : _paddedSize;
            return Composition.ComposeImage(colors, imageSize, _swizzle);
        }

        public Image Decode(byte[] data, byte[] paletteData)
        {
            // Load palette indexColors
            var palette = _paletteEncoding.Load(paletteData).ToList();

            // Load image indexColors
            var colors = _indexEncoding.Load(data, palette);

            // Compose image
            var imageSize = _paddedSize == Size.Empty ? _imageSize : _paddedSize;
            return Composition.ComposeImage(colors, imageSize, _swizzle);
        }

        byte[] IColorTranscoder.Encode(Bitmap image)
        {
            // If we have quantization enabled
            IEnumerable<Color> colors;
            if (_quantizer != null)
            {
                var (indices, palette) = QuantizeImage(image);

                // Recompose indices to colors
                colors = indices.Select(index => palette[index]);
            }
            else
            {
                var imageSize = _paddedSize == Size.Empty ? image.Size : _paddedSize;

                // Decompose image to colors
                colors = Composition.DecomposeImage(image, imageSize, _swizzle);
            }

            // Save color data
            return _colorEncoding.Save(colors);
        }

        // ReSharper disable PossibleMultipleEnumeration
        (byte[] indexData, byte[] paletteData) IIndexTranscoder.Encode(Bitmap image)
        {
            var (indices, palette) = QuantizeImage(image);

            // Save palette indexColors
            var paletteData = _paletteEncoding.Save(palette);

            // Save image indexColors
            var indexData = _indexEncoding.Save(indices, palette);

            return (indexData, paletteData);
        }

        private (IEnumerable<int> indices, IList<Color> palette) QuantizeImage(Bitmap image)
        {
            var imageSize = _paddedSize == Size.Empty ? image.Size : _paddedSize;

            // Decompose unswizzled image to colors
            var colors = Composition.DecomposeImage(image, imageSize);

            // Quantize unswizzled indexColors
            var (indices, palette) = _quantizer.Process(colors, imageSize);

            // Swizzle indexColors to correct positions
            indices = SwizzleIndices(indices, imageSize, _swizzle);

            return (indices, palette);
        }

        private IEnumerable<int> SwizzleIndices(IEnumerable<int> indeces, Size imageSize, IImageSwizzle swizzle)
        {
            var indexPoints = Zip(indeces, Composition.GetPointSequence(imageSize, swizzle));
            return indexPoints.OrderBy(cp => GetIndex(cp.Second, imageSize)).Select(x => x.First);
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
