﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.Progress;
using Kontract.Kanvas;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Model;
using Kontract.Kanvas.Quantization;
using Kontract.Models.Image;

namespace Kanvas.Configuration
{
    // TODO: For better management, add a container for all the different configuration variables. It may be set up by the configuration, and passed into the transcoder
    /// <summary>
    /// The class to implement transcoding actions on data and images.
    /// </summary>
    class ImageTranscoder : IImageTranscoder
    {
        private readonly int _taskCount;

        private readonly CreatePixelRemapper _remapPixels;
        private readonly IPadSizeOptionsBuild _padSizeOptions;
        private readonly CreateShadedColor _shadeColorsFunc;

        private readonly IIndexEncoding _indexEncoding;
        private readonly IColorEncoding _paletteEncoding;
        private readonly IQuantizer _quantizer;

        private readonly ImageAnchor _anchor;

        private readonly IColorEncoding _colorEncoding;

        private bool IsIndexed => _indexEncoding != null && _paletteEncoding != null;

        /// <summary>
        /// Creates a new instance of <see cref="ImageTranscoder"/> for usage on indexed images.
        /// </summary>
        /// <param name="indexEncoding"></param>
        /// <param name="paletteEncoding"></param>
        /// <param name="remapPixels"></param>
        /// <param name="padSizeOptions"></param>
        /// <param name="shadeColorsFunc"></param>
        /// <param name="anchor"></param>
        /// <param name="quantizer"></param>
        /// <param name="taskCount"></param>
        public ImageTranscoder(IIndexEncoding indexEncoding, IColorEncoding paletteEncoding,
            CreatePixelRemapper remapPixels, IPadSizeOptionsBuild padSizeOptions, CreateShadedColor shadeColorsFunc,
            ImageAnchor anchor, IQuantizer quantizer, int taskCount)
        {
            ContractAssertions.IsNotNull(indexEncoding, nameof(indexEncoding));
            ContractAssertions.IsNotNull(quantizer, nameof(quantizer));

            // HINT: paletteEncoding can be null due to EncodeIndexInternal handling it.

            _indexEncoding = indexEncoding;
            _paletteEncoding = paletteEncoding;
            _quantizer = quantizer;

            _remapPixels = remapPixels;
            _padSizeOptions = padSizeOptions;
            _shadeColorsFunc = shadeColorsFunc;

            _anchor = anchor;

            _taskCount = taskCount;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ImageTranscoder"/> for usage on non-indexed images.
        /// </summary>
        /// <param name="colorEncoding"></param>
        /// <param name="remapPixels"></param>
        /// <param name="padSizeOptionsFunc"></param>
        /// <param name="shadeColorsFunc"></param>
        /// <param name="anchor"></param>
        /// <param name="quantizer"></param>
        /// <param name="taskCount"></param>
        public ImageTranscoder(IColorEncoding colorEncoding, CreatePixelRemapper remapPixels,
            IPadSizeOptionsBuild padSizeOptionsFunc, CreateShadedColor shadeColorsFunc,
            ImageAnchor anchor, IQuantizer quantizer, int taskCount)
        {
            ContractAssertions.IsNotNull(colorEncoding, nameof(colorEncoding));

            _colorEncoding = colorEncoding;
            _quantizer = quantizer;

            _remapPixels = remapPixels;
            _padSizeOptions = padSizeOptionsFunc;
            _shadeColorsFunc = shadeColorsFunc;

            _anchor = anchor;

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
            // Prepare information and instances
            var paddedSize = GetPaddedSize(imageSize);
            var swizzle = GetPixelRemapper(_colorEncoding, paddedSize);
            var finalSize = GetFinalSize(paddedSize, swizzle);
            var colorShader = _shadeColorsFunc?.Invoke();

            // Load colors
            var colorCount = data.Length * 8 / _colorEncoding.BitsPerValue * _colorEncoding.ColorsPerValue;
            var colorCountBySize = finalSize.Width * finalSize.Height;

            // HINT: If the data portion does not fit with the actual image size, it will cause progress irregularities.
            //       If the given data is shorter than what is needed for the full image, we throw.
            //       Otherwise enough data is given and the image can be fully decoded, even if excess data is not used.
            if (colorCount < colorCountBySize)
                throw new InvalidOperationException("Given data is too short.");

            var setMaxProgress = progress?.SetMaxValue(colorCountBySize * _colorEncoding.ColorsPerValue);
            var colors = _colorEncoding
                .Load(data, new EncodingLoadContext(finalSize, _taskCount))
                .AttachProgress(setMaxProgress, "Decode colors");

            // Apply color shader
            if (colorShader != null)
                colors = colors.Select(colorShader.Read);

            // Create image with unpadded dimensions
            return colors.ToBitmap(imageSize, paddedSize, swizzle, _anchor);
        }

        private Bitmap DecodeIndexInternal(byte[] data, byte[] paletteData, Size imageSize, IProgressContext progress)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            var progresses = progress?.SplitIntoEvenScopes(2);

            // Prepare information and instances
            var paddedSize = GetPaddedSize(imageSize);
            var swizzle = GetPixelRemapper(_indexEncoding, paddedSize);
            var finalSize = GetFinalSize(paddedSize, swizzle);
            var colorShader = _shadeColorsFunc?.Invoke();

            // Load palette
            var paletteColorCount = paletteData.Length * 8 / _paletteEncoding.BitsPerValue * _paletteEncoding.ColorsPerValue;

            var setMaxProgress = progresses?[0]?.SetMaxValue(paletteColorCount * _paletteEncoding.ColorsPerValue);
            var paletteEnumeration = _paletteEncoding
                .Load(paletteData, new EncodingLoadContext(new Size(1, paletteColorCount), _taskCount))
                .AttachProgress(setMaxProgress, "Decode palette colors");

            // Apply color shader on palette
            var palette = colorShader != null ? paletteEnumeration.Select(colorShader.Read).ToArray() : paletteEnumeration.ToArray();

            // Load indices
            setMaxProgress = progresses?[1]?.SetMaxValue(finalSize.Width * finalSize.Height);
            var colors = _indexEncoding
                .Load(data, palette, new EncodingLoadContext(finalSize, _taskCount))
                .AttachProgress(setMaxProgress, "Decode colors");

            return colors.ToBitmap(imageSize, paddedSize, swizzle, _anchor);
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
            // Prepare information and instances
            var paddedSize = GetPaddedSize(image.Size);
            var swizzle = GetPixelRemapper(_colorEncoding, paddedSize);
            var finalSize = GetFinalSize(paddedSize, swizzle);
            var colorShader = _shadeColorsFunc?.Invoke();

            // If we have quantization enabled
            IEnumerable<Color> colors;
            if (_quantizer != null)
            {
                var scopedProgresses = progress?.SplitIntoEvenScopes(2);

                // HINT: Color shader is applied by QuantizeImage
                var (indices, palette) = QuantizeImage(image, paddedSize, swizzle, scopedProgresses?[0]);

                // Recompose indices to colors
                var setMaxProgress = scopedProgresses?[1]?.SetMaxValue(finalSize.Width * finalSize.Height);
                colors = indices.ToColors(palette).AttachProgress(setMaxProgress, "Encode indices");
            }
            else
            {
                // Decompose image to colors
                var setMaxProgress = progress?.SetMaxValue(finalSize.Width * finalSize.Height);
                colors = image.ToColors(paddedSize, swizzle, _anchor).AttachProgress(setMaxProgress, "Encode colors");

                // Apply color shader
                if (colorShader != null)
                    colors = colors.Select(colorShader.Write);
            }

            // Save color data
            return _colorEncoding.Save(colors, new EncodingSaveContext(finalSize, _taskCount));
        }

        private (byte[] indexData, byte[] paletteData) EncodeIndexInternal(Bitmap image, IProgressContext progress = null)
        {
            // Prepare information and instances
            var paddedSize = GetPaddedSize(image.Size);
            var swizzle = GetPixelRemapper(_indexEncoding, paddedSize);
            var finalSize = GetFinalSize(paddedSize, swizzle);

            var (indices, palette) = QuantizeImage(image, paddedSize, swizzle, progress);

            // Save palette colors
            // This step can be skipped if no palette encoding is given.
            //   That saves time in the scenario when the palette is not needed or already exists as encoded data from somewhere else.
            var paletteData = _paletteEncoding?.Save(palette, new EncodingSaveContext(new Size(1, palette.Count), _taskCount));

            // Save image indexColors
            var indexData = _indexEncoding.Save(indices, palette, new EncodingSaveContext(finalSize, _taskCount));

            return (indexData, paletteData);
        }

        #endregion

        private (IEnumerable<int> indices, IList<Color> palette) QuantizeImage(Bitmap image, Size paddedSize, IImageSwizzle swizzle, IProgressContext progress = null)
        {
            var finalSize = GetFinalSize(paddedSize, swizzle);
            var colorShader = _shadeColorsFunc?.Invoke();

            // Decompose unswizzled image to colors
            var colors = image.ToColors(paddedSize);

            // Quantize unswizzled indices
            var (indices, palette) = _quantizer.Process(colors, finalSize, progress);

            // Apply color shader
            if (colorShader != null)
                palette = palette.Select(colorShader.Write).ToArray();

            // Delegate indices to correct positions
            var swizzledIndices = SwizzleIndices(indices.ToArray(), finalSize, swizzle);

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

        #region Support

        private Size GetPaddedSize(Size imageSize)
        {
            return _padSizeOptions.Build(imageSize);
        }

        private Size GetFinalSize(Size paddedSize, IImageSwizzle swizzle)
        {
            // Swizzle dimensions are based on padded size already
            // Swizzle has higher priority since it might pad the padded size further, due to its macro blocks
            if (swizzle != null)
                return new Size(swizzle.Width, swizzle.Height);

            // Otherwise just return the already padded size
            return paddedSize;
        }

        private IImageSwizzle GetPixelRemapper(IEncodingInfo encodingInfo, Size paddedSize)
        {
            return _remapPixels?.Invoke(new SwizzlePreparationContext(encodingInfo, paddedSize));
        }

        #endregion
    }
}
