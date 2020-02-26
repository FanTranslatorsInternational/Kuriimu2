using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontract;
using Kontract.Kanvas;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    class Transcoder : IColorTranscoder, IIndexTranscoder
    {
        private readonly int _taskCount;

        private CreatePixelRemapper _swizzle;

        private CreateColorIndexEncoding _indexEncoding;
        private CreatePaletteEncoding _paletteEncoding;
        private IQuantizer _quantizer;

        private CreateColorEncoding _colorEncoding;

        public Transcoder(CreateColorIndexEncoding indexEncoding, CreatePaletteEncoding paletteEncoding,
            CreatePixelRemapper swizzle, IQuantizer quantizer, int taskCount)
        {
            ContractAssertions.IsNotNull(indexEncoding, nameof(indexEncoding));
            ContractAssertions.IsNotNull(paletteEncoding, nameof(paletteEncoding));
            ContractAssertions.IsNotNull(quantizer, nameof(quantizer));

            _indexEncoding = indexEncoding;
            _paletteEncoding = paletteEncoding;
            _swizzle = swizzle;
            _quantizer = quantizer;

            _taskCount = taskCount;
        }

        public Transcoder(CreateColorEncoding colorEncoding, CreatePixelRemapper swizzle,
            IQuantizer quantizer, int taskCount)
        {
            ContractAssertions.IsNotNull(colorEncoding, nameof(colorEncoding));

            _colorEncoding = colorEncoding;
            _swizzle = swizzle;
            _quantizer = quantizer;

            _taskCount = taskCount;
        }

        public Image Decode(byte[] data, Size imageSize) =>
            Decode(data, imageSize, Size.Empty);

        public Image Decode(byte[] data, Size imageSize, Size paddedSize)
        {
            // Load colors
            // TODO: Size is currently only used for block compression with native libs,
            // TODO: Those libs should retrieve the actual size of the image, not the padded dimensions
            var colors = _colorEncoding(imageSize).Load(data, _taskCount);

            // Compose image
            var size = paddedSize.IsEmpty ? imageSize : paddedSize;
            return colors.ToBitmap(size, _swizzle?.Invoke(size));
        }

        public Image Decode(byte[] data, byte[] paletteData, Size imageSize) =>
            Decode(data, paletteData, imageSize, Size.Empty);

        public Image Decode(byte[] data, byte[] paletteData, Size imageSize, Size paddedSize)
        {
            // Load palette indexColors
            var palette = _paletteEncoding().Load(paletteData, _taskCount).ToList();

            // Load image indexColors
            // TODO: Size is currently only used for block compression with native libs,
            // TODO: Those libs should retrieve the actual size of the image, not the padded dimensions
            // Yes, this even applies for index encodings, just in case
            var colors = _indexEncoding(imageSize).Load(data, palette, _taskCount);

            // Compose image
            var size = paddedSize.IsEmpty ? imageSize : paddedSize;
            return colors.ToBitmap(size, _swizzle?.Invoke(size));
        }

        byte[] IColorTranscoder.Encode(Bitmap image) =>
            EncodeInternal(image, Size.Empty);

        byte[] IColorTranscoder.Encode(Bitmap image, Size paddedSize) =>
            EncodeInternal(image, paddedSize);

        private byte[] EncodeInternal(Bitmap image, Size paddedSize)
        {
            // If we have quantization enabled
            IEnumerable<Color> colors;
            if (_quantizer != null)
            {
                var (indices, palette) = QuantizeImage(image, paddedSize);

                // Recompose indices to colors
                colors = indices.Select(index => palette[index]);
            }
            else
            {
                // Decompose image to colors
                colors = image.ToColors(paddedSize, _swizzle?.Invoke(paddedSize));
            }

            // Save color data
            var size = paddedSize.IsEmpty ? image.Size : paddedSize;
            return _colorEncoding(size).Save(colors, _taskCount);
        }

        (byte[] indexData, byte[] paletteData) IIndexTranscoder.Encode(Bitmap image) =>
            EncodeIndexInternal(image, Size.Empty);

        // ReSharper disable PossibleMultipleEnumeration
        (byte[] indexData, byte[] paletteData) IIndexTranscoder.Encode(Bitmap image, Size paddedSize) =>
            EncodeIndexInternal(image, paddedSize);

        private (byte[] indexData, byte[] paletteData) EncodeIndexInternal(Bitmap image, Size paddedSize)
        {
            var (indices, palette) = QuantizeImage(image, paddedSize);

            // Save palette indexColors
            var paletteData = _paletteEncoding().Save(palette, _taskCount);

            // Save image indexColors
            var size = paddedSize.IsEmpty ? image.Size : paddedSize;
            var indexData = _indexEncoding(size).Save(indices, palette, _taskCount);

            return (indexData, paletteData);
        }

        private (IEnumerable<int> indices, IList<Color> palette) QuantizeImage(Bitmap image, Size paddedSize)
        {
            var imageSize = paddedSize.IsEmpty ? image.Size : paddedSize;

            // Decompose unswizzled image to colors
            var colors = image.ToColors(paddedSize);

            // Quantize unswizzled indexColors
            var (indices, palette) = _quantizer.Process(colors, imageSize);

            // Swizzle indexColors to correct positions
            indices = SwizzleIndices(indices, imageSize, _swizzle?.Invoke(imageSize));

            return (indices, palette);
        }

        private IEnumerable<int> SwizzleIndices(IEnumerable<int> indices, Size imageSize, IImageSwizzle swizzle)
        {
            var indexPoints = Zip(indices, Composition.GetPointSequence(imageSize, swizzle));
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
