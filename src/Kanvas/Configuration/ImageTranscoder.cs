using System;
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
    /// <summary>
    /// The class to implement transcoding actions on data and images.
    /// </summary>
    class ImageTranscoder : IImageTranscoder
    {
        private readonly int _taskCount;

        private readonly CreatePixelRemapper _swizzle;
        private readonly CreatePaddedSize _paddedSize;

        private readonly CreateIndexEncoding _indexEncoding;
        private readonly CreatePaletteEncoding _paletteEncoding;
        private readonly IQuantizer _quantizer;

        private readonly CreateColorEncoding _colorEncoding;

        private bool IsIndexed => _indexEncoding != null && _paletteEncoding != null;

        /// <summary>
        /// Creates a new instance of <see cref="ImageTranscoder"/> for usage on indexed images.
        /// </summary>
        /// <param name="indexEncoding"></param>
        /// <param name="paletteEncoding"></param>
        /// <param name="swizzle"></param>
        /// <param name="paddedSizeFunc"></param>
        /// <param name="quantizer"></param>
        /// <param name="taskCount"></param>
        public ImageTranscoder(CreateIndexEncoding indexEncoding, CreatePaletteEncoding paletteEncoding,
            CreatePixelRemapper swizzle, CreatePaddedSize paddedSizeFunc,
            IQuantizer quantizer, int taskCount)
        {
            ContractAssertions.IsNotNull(indexEncoding, nameof(indexEncoding));
            ContractAssertions.IsNotNull(quantizer, nameof(quantizer));

            // HINT: paletteEncoding can be null due to EncodeIndexInternal handling it.

            _indexEncoding = indexEncoding;
            _paletteEncoding = paletteEncoding;
            _quantizer = quantizer;

            _swizzle = swizzle;
            _paddedSize = paddedSizeFunc;

            _taskCount = taskCount;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ImageTranscoder"/> for usage on non-indexed images.
        /// </summary>
        /// <param name="colorEncoding"></param>
        /// <param name="swizzle"></param>
        /// <param name="paddedSizeFunc"></param>
        /// <param name="quantizer"></param>
        /// <param name="taskCount"></param>
        public ImageTranscoder(CreateColorEncoding colorEncoding, CreatePixelRemapper swizzle,
            CreatePaddedSize paddedSizeFunc,
            IQuantizer quantizer, int taskCount)
        {
            ContractAssertions.IsNotNull(colorEncoding, nameof(colorEncoding));

            _colorEncoding = colorEncoding;
            _quantizer = quantizer;

            _swizzle = swizzle;
            _paddedSize = paddedSizeFunc;

            _taskCount = taskCount;
        }

        #region Decode methods

        public Bitmap Decode(byte[] data, Size imageSize, IProgressContext progress = null) =>
            Decode(data, null, imageSize, progress);

        public Bitmap Decode(byte[] data, byte[] paletteData, Size imageSize, IProgressContext progress = null)
        {
            if (IsIndexed && paletteData == null)
                throw new ArgumentNullException(nameof(paletteData));

            return IsIndexed ?
                DecodeIndexInternal(data, paletteData, imageSize, progress) :
                DecodeColorInternal(data, imageSize, progress);
        }

        private Bitmap DecodeColorInternal(byte[] data, Size imageSize, IProgressContext progress)
        {
            var paddedSize = _paddedSize?.Invoke(imageSize) ?? Size.Empty;
            var finalSize = paddedSize.IsEmpty ? imageSize : paddedSize;

            var colorEncoding = _colorEncoding(imageSize);

            // Load colors
            // TODO: Size is currently only used for block compression with native libs,
            // TODO: Those libs should retrieve the actual size of the image, not the padded dimensions
            var valueCount = data.Length * 8 / colorEncoding.BitsPerValue;
            var valueCountBySize = finalSize.Width * finalSize.Height / colorEncoding.ColorsPerValue;

            // HINT: If the data portion does not fit with the actual image size, it will cause progress irregularities.
            //       If the given data is shorter than what is needed  for the full image, we throw.
            //       Otherwise enough data is given and the image can be fully decoded, even if excess data is not used.
            if (valueCount < valueCountBySize)
                throw new InvalidOperationException("Given data is too short.");

            var setMaxProgress = progress?.SetMaxValue(valueCountBySize * colorEncoding.ColorsPerValue);
            var colors = colorEncoding
                .Load(data, _taskCount)
                .AttachProgress(setMaxProgress, "Decode colors");

            // Create image with unpadded dimensions
            return colors.ToBitmap(imageSize, paddedSize, _swizzle?.Invoke(finalSize));
        }

        private Bitmap DecodeIndexInternal(byte[] data, byte[] paletteData, Size imageSize, IProgressContext progress)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            var progresses = progress.SplitIntoEvenScopes(2);

            var paddedSize = _paddedSize?.Invoke(imageSize) ?? Size.Empty;

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

            return colors.ToBitmap(imageSize, _swizzle?.Invoke(paddedSize.IsEmpty ? imageSize : paddedSize));
        }

        #endregion

        #region Encode methods

        public (byte[] imageData, byte[] paletteData) Encode(Bitmap image, IProgressContext progress = null)
        {
            return IsIndexed ?
                EncodeIndexInternal(image, progress) :
                (EncodeColorInternal(image, progress), null);
        }

        private byte[] EncodeColorInternal(Bitmap image, IProgressContext progress = null)
        {
            var paddedSize = _paddedSize?.Invoke(image.Size) ?? Size.Empty;
            var size = paddedSize.IsEmpty ? image.Size : paddedSize;

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
                colors = image.ToColors(paddedSize, _swizzle?.Invoke(size)).AttachProgress(setMaxProgress, "Encode colors");
            }

            // Save color data
            return _colorEncoding(image.Size).Save(colors, _taskCount);
        }

        private (byte[] indexData, byte[] paletteData) EncodeIndexInternal(Bitmap image, IProgressContext progress = null)
        {
            var paddedSize = _paddedSize?.Invoke(image.Size) ?? Size.Empty;

            var (indices, palette) = QuantizeImage(image, paddedSize, progress);

            // Save palette indexColors
            // This step can be skipped if no palette encoding is given.
            //   That saves time in the scenario when the palette is not needed or already exists as encoded data from somewhere else.
            var paletteData = _paletteEncoding?.Invoke().Save(palette, _taskCount);

            // Save image indexColors
            var size = paddedSize.IsEmpty ? image.Size : paddedSize;
            var indexData = _indexEncoding(size).Save(indices, palette, _taskCount);

            return (indexData, paletteData);
        }

        #endregion

        private (IEnumerable<int> indices, IList<Color> palette) QuantizeImage(Bitmap image, Size paddedSize, IProgressContext progress = null)
        {
            var imageSize = paddedSize.IsEmpty ? image.Size : paddedSize;

            // Decompose unswizzled image to colors
            var colors = image.ToColors(paddedSize);

            // Quantize unswizzled indices
            var (indices, palette) = _quantizer.Process(colors, imageSize, progress);

            // Swizzle indices to correct positions
            var swizzledIndices = SwizzleIndices(indices.ToArray(), imageSize, _swizzle?.Invoke(imageSize));

            return (swizzledIndices, palette);
        }

        private IEnumerable<int> SwizzleIndices(IList<int> indices, Size imageSize, IImageSwizzle swizzle)
        {
            return Composition.GetPointSequence(imageSize, swizzle)
                .Select(point => indices[GetIndex(point, imageSize)]);
        }

        private int GetIndex(Point point, Size imageSize)
        {
            return point.Y * imageSize.Width + point.X;
        }
    }
}
