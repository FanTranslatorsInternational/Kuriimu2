using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.Progress;
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

        #region Decode interface methods

        public Image Decode(byte[] data, Size imageSize, IProgressContext progress = null) =>
            Decode(data, imageSize, Size.Empty, progress);

        public Image Decode(byte[] data, Size imageSize, Size paddedSize, IProgressContext progress = null) =>
            DecodeInternal(data, imageSize, paddedSize, progress);

        public Image Decode(byte[] data, byte[] paletteData, Size imageSize, IProgressContext progress = null) =>
            Decode(data, paletteData, imageSize, Size.Empty, progress);

        public Image Decode(byte[] data, byte[] paletteData, Size imageSize, Size paddedSize,
            IProgressContext progress = null) =>
            DecodeIndexInternal(data, paletteData, imageSize, paddedSize, progress);

        #endregion

        #region Encode interface methods

        byte[] IColorTranscoder.Encode(Bitmap image, IProgressContext progress = null) =>
            EncodeInternal(image, Size.Empty, progress);

        byte[] IColorTranscoder.Encode(Bitmap image, Size paddedSize, IProgressContext progress = null) =>
            EncodeInternal(image, paddedSize, progress);

        (byte[] indexData, byte[] paletteData) IIndexTranscoder.Encode(Bitmap image, IProgressContext progress = null) =>
            EncodeIndexInternal(image, Size.Empty, progress);

        // ReSharper disable PossibleMultipleEnumeration
        (byte[] indexData, byte[] paletteData) IIndexTranscoder.Encode(Bitmap image, Size paddedSize, IProgressContext progress = null) =>
            EncodeIndexInternal(image, paddedSize, progress);

        #endregion

        private Image DecodeInternal(byte[] data, Size imageSize, Size paddedSize,
            IProgressContext progress = null)
        {
            var size = paddedSize.IsEmpty ? imageSize : paddedSize;

            var colorEncoding = _colorEncoding(imageSize);

            // Load colors
            // TODO: Size is currently only used for block compression with native libs,
            // TODO: Those libs should retrieve the actual size of the image, not the padded dimensions
            var valueCount = data.Length * 8 / colorEncoding.BitsPerValue;
            var setMaxProgress = progress?.SetMaxValue(valueCount * colorEncoding.ColorsPerValue);
            var colors = colorEncoding
                .Load(data, _taskCount)
                .AttachProgress(setMaxProgress, "Decode colors");

            // Create image
            return colors.ToBitmap(size, _swizzle?.Invoke(size));
        }

        private Image DecodeIndexInternal(byte[] data, byte[] paletteData, Size imageSize, Size paddedSize,
            IProgressContext progress = null)
        {
            var progresses = progress.SplitIntoEvenScopes(2);

            var size = paddedSize.IsEmpty ? imageSize : paddedSize;

            var paletteEncoding = _paletteEncoding();
            var indexEncoding = _indexEncoding(imageSize);

            // Load palette
            var valueCount = data.Length * 8 / paletteEncoding.BitsPerValue;
            var setMaxProgress = progresses?[0]?.SetMaxValue(valueCount * paletteEncoding.ColorsPerValue);
            var palette = paletteEncoding
                .Load(paletteData, _taskCount)
                .AttachProgress(setMaxProgress, "Decode palette colors")
                .ToList();

            // Load indices
            // TODO: Size is currently only used for block compression with native libs,
            // TODO: Those libs should retrieve the actual size of the image, not the padded dimensions
            // Yes, this even applies for index encodings, just in case
            valueCount = data.Length * 8 / indexEncoding.BitsPerValue;
            setMaxProgress = progresses?[1]?.SetMaxValue(valueCount * indexEncoding.ColorsPerValue);
            var colors = indexEncoding
                .Load(data, palette, _taskCount)
                .AttachProgress(setMaxProgress, "Decode colors");

            return colors.ToBitmap(size, _swizzle?.Invoke(size));
        }

        private byte[] EncodeInternal(Bitmap image, Size paddedSize, IProgressContext progress = null)
        {
            var imageSize = paddedSize.IsEmpty ? image.Size : paddedSize;

            // If we have quantization enabled
            IEnumerable<Color> colors;
            if (_quantizer != null)
            {
                var scopedProgresses = progress?.SplitIntoEvenScopes(2);

                var (indices, palette) = QuantizeImage(image, paddedSize, scopedProgresses?[0]);

                // Recompose indices to colors
                var setMaxProgress = scopedProgresses?[1]?.SetMaxValue(image.Width * image.Height);
                colors = indices.ToColors(palette).AttachProgress(setMaxProgress, "Encode indices");
            }
            else
            {
                // Decompose image to colors
                var setMaxProgress = progress?.SetMaxValue(image.Width * image.Height);
                colors = image.ToColors(paddedSize, _swizzle?.Invoke(imageSize)).AttachProgress(setMaxProgress, "Encode colors");
            }

            // Save color data
            return _colorEncoding(imageSize).Save(colors, _taskCount);
        }

        private (byte[] indexData, byte[] paletteData) EncodeIndexInternal(Bitmap image, Size paddedSize, IProgressContext progress = null)
        {
            var (indices, palette) = QuantizeImage(image, paddedSize, progress);

            // Save palette indexColors
            var paletteData = _paletteEncoding().Save(palette, _taskCount);

            // Save image indexColors
            var size = paddedSize.IsEmpty ? image.Size : paddedSize;
            var indexData = _indexEncoding(size).Save(indices, palette, _taskCount);

            return (indexData, paletteData);
        }

        private (IEnumerable<int> indices, IList<Color> palette) QuantizeImage(Bitmap image, Size paddedSize, IProgressContext progress = null)
        {
            var imageSize = paddedSize.IsEmpty ? image.Size : paddedSize;

            // Decompose unswizzled image to colors
            var colors = image.ToColors(paddedSize);

            // Quantize unswizzled indexColors
            var (indices, palette) = _quantizer.Process(colors, imageSize, progress);

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
