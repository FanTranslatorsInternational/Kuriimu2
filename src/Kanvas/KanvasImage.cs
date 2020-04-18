using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Configuration;
using Kontract;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Models.Image;
using Kontract.Kanvas;
using Kontract.Kanvas.Configuration;

namespace Kanvas
{
    /// <summary>
    /// The class to wrap an image plugin of the Kuriimu2 runtime to work with its actual image.
    /// Is not necessary for the Kanvas library itself to function properly.
    /// </summary>
    public sealed class KanvasImage : IKanvasImage
    {
        private readonly IImageState _imageState;
        private readonly ImageInfo _imageInfo;

        private Bitmap _decodedImage;
        private Bitmap _bestImage;
        private IList<Color> _decodedPalette;

        private IImageConfiguration ImageConfiguration =>
            _imageInfo.Configuration ?? new ImageConfiguration();

        /// <inheritdoc />
        public bool IsIndexed => IsIndexEncoding(_imageInfo.ImageFormat);

        /// <inheritdoc />
        public int ImageFormat => _imageInfo.ImageFormat;

        /// <inheritdoc />
        public int PaletteFormat => _imageInfo.PaletteFormat;

        /// <summary>
        /// Creates a new instance of <see cref="KanvasImage"/>.
        /// </summary>
        /// <param name="imageState">The image plugin the plugin information come from.</param>
        /// <param name="imageInfo">The image info to represent.</param>
        public KanvasImage(IImageState imageState, ImageInfo imageInfo)
        {
            ContractAssertions.IsNotNull(imageState, nameof(imageState));
            ContractAssertions.IsNotNull(imageInfo, nameof(imageInfo));
            ContractAssertions.IsElementContained(imageState.Images, imageInfo, nameof(imageState.Images), nameof(imageInfo));

            _imageState = imageState;
            _imageInfo = imageInfo;
        }

        /// <inheritdoc />
        public Bitmap GetImage(IProgressContext progress = null)
        {
            return DecodeImage(progress);
        }

        /// <inheritdoc />
        public IList<Color> GetPalette(IProgressContext progress = null)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            return DecodePalette(progress);
        }

        /// <inheritdoc />
        public void SetImage(Bitmap image, IProgressContext progress = null)
        {
            _decodedImage = _bestImage = image;
            _decodedPalette = null;

            (_imageInfo.ImageData, _imageInfo.PaletteData) = EncodeImage(image, _imageInfo.ImageFormat, _imageInfo.PaletteFormat, progress);
            _imageInfo.ImageSize = image.Size;
        }

        /// <inheritdoc />
        public void SetPalette(IList<Color> palette, IProgressContext progress = null)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            _decodedImage = _bestImage = null;
            _decodedPalette = palette;

            _imageInfo.PaletteData = EncodePalette(palette, _imageInfo.PaletteFormat);
        }

        /// <inheritdoc />
        public void TranscodeImage(int imageFormat, IProgressContext progress = null)
        {
            var paletteFormat = _imageInfo.PaletteFormat;
            if (!IsIndexed && IsIndexEncoding(imageFormat))
                paletteFormat = _imageState.SupportedIndexEncodings[imageFormat].PaletteEncodingIndices?.First() ??
                               _imageState.SupportedPaletteEncodings.First().Key;

            TranscodeInternal(imageFormat, paletteFormat, true, progress);
        }

        /// <inheritdoc />
        public void TranscodePalette(int paletteFormat, IProgressContext progress = null)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            TranscodeInternal(_imageInfo.ImageFormat, paletteFormat, true, progress);
        }

        /// <inheritdoc />
        public void SetColorInPalette(int paletteIndex, Color color)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            var palette = DecodePalette();
            if (paletteIndex >= palette.Count)
                throw new InvalidOperationException($"Palette index {paletteIndex} is not in range.");

            palette[paletteIndex] = color;
            SetPalette(palette);
        }

        /// <inheritdoc />
        public void SetIndexInImage(Point point, int paletteIndex)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            var image = DecodeImage();
            if (!IsPointInRegion(point, image.Size))
                throw new InvalidOperationException($"Point {point} is not in image.");

            var palette = DecodePalette();
            if (paletteIndex >= palette.Count)
                throw new InvalidOperationException($"Palette index {paletteIndex} is not in range.");

            image.SetPixel(point.X, point.Y, palette[paletteIndex]);

            _decodedImage = image;
            (_imageInfo.ImageData, _imageInfo.PaletteData) = EncodeImage(image, _imageInfo.ImageFormat, _imageInfo.PaletteFormat);
        }

        private void TranscodeInternal(int imageFormat, int paletteFormat, bool checkFormatEquality, IProgressContext progress = null)
        {
            AssertImageFormatExists(imageFormat);
            if (IsIndexEncoding(imageFormat))
                AssertPaletteFormatExists(paletteFormat);

            if (checkFormatEquality)
                if (_imageInfo.ImageFormat == imageFormat &&
                    IsIndexEncoding(imageFormat) && _imageInfo.PaletteFormat == paletteFormat)
                    return;

            var decodedImage = _bestImage ?? DecodeImage(progress);
            var (imageData, paletteData) = EncodeImage(decodedImage, imageFormat, paletteFormat, progress);

            // Set image info properties
            _imageInfo.ImageFormat = imageFormat;
            _imageInfo.ImageData = imageData;
            _imageInfo.ImageSize = decodedImage.Size;

            if (IsIndexEncoding(imageFormat))
            {
                _imageInfo.PaletteFormat = paletteFormat;
                _imageInfo.PaletteData = paletteData;
            }
            else
            {
                _imageInfo.PaletteFormat = -1;
                _imageInfo.PaletteData = null;
            }

            _decodedImage = null;
            _decodedPalette = null;
        }

        private Bitmap DecodeImage(IProgressContext progress = null)
        {
            if (_decodedImage != null)
                return _decodedImage;

            Func<Bitmap> decodeImageAction;
            if (IsIndexed)
            {
                var transcoder = ImageConfiguration.Clone()
                    .TranscodeWith(size => _imageState.SupportedIndexEncodings[_imageInfo.ImageFormat].Encoding)
                    .TranscodePaletteWith(() => _imageState.SupportedPaletteEncodings[_imageInfo.PaletteFormat])
                    .Build();

                decodeImageAction = () => transcoder.Decode(_imageInfo.ImageData, _imageInfo.PaletteData, _imageInfo.ImageSize, progress);
            }
            else
            {
                var transcoder = ImageConfiguration.Clone()
                    .TranscodeWith(size => _imageState.SupportedEncodings[_imageInfo.ImageFormat])
                    .Build();

                decodeImageAction = () => transcoder.Decode(_imageInfo.ImageData, _imageInfo.ImageSize, progress);
            }

            _decodedImage = decodeImageAction();
            if (_bestImage == null)
                _bestImage = _decodedImage;

            return _decodedImage;
        }

        private IList<Color> DecodePalette(IProgressContext context = null)
        {
            if (_decodedPalette != null)
                return _decodedPalette;

            return _decodedPalette = _imageState.SupportedPaletteEncodings[_imageInfo.PaletteFormat]
                .Load(_imageInfo.PaletteData, Environment.ProcessorCount)
                .ToArray();
        }

        private (byte[] imageData, byte[] paletteData) EncodeImage(Bitmap image, int imageFormat, int paletteFormat = -1,
            IProgressContext progress = null)
        {
            IImageTranscoder transcoder;
            if (IsColorEncoding(imageFormat))
            {
                transcoder = ImageConfiguration.Clone()
                    .TranscodeWith(size => _imageState.SupportedEncodings[imageFormat])
                    .Build();
            }
            else
            {
                var indexEncoding = _imageState.SupportedIndexEncodings[imageFormat].Encoding;
                transcoder = ImageConfiguration.Clone()
                    .ConfigureQuantization(options => options.WithColorCount(indexEncoding.MaxColors))
                    .TranscodeWith(size => indexEncoding)
                    .TranscodePaletteWith(() => _imageState.SupportedPaletteEncodings[paletteFormat])
                    .Build();
            }

            return transcoder.Encode(image, progress);
        }

        private byte[] EncodePalette(IList<Color> palette, int paletteFormat)
        {
            return _imageState.SupportedPaletteEncodings[paletteFormat]
                .Save(palette, Environment.ProcessorCount);
        }

        private void AssertImageFormatExists(int imageFormat)
        {
            var supportedFormats = new List<int>();
            if (_imageState.SupportedEncodings != null)
                supportedFormats.AddRange(_imageState.SupportedEncodings.Keys);
            if (_imageState.SupportedIndexEncodings != null)
                supportedFormats.AddRange(_imageState.SupportedIndexEncodings.Keys);

            if (!supportedFormats.Contains(imageFormat))
                throw new InvalidOperationException($"The image format '{imageFormat}' is not supported by the plugin.");
        }

        private void AssertPaletteFormatExists(int paletteFormat)
        {
            var supportedPaletteFormats = new List<int>();
            if (_imageState.SupportedPaletteEncodings != null)
                supportedPaletteFormats.AddRange(_imageState.SupportedPaletteEncodings.Keys);

            if (!supportedPaletteFormats.Contains(paletteFormat))
                throw new InvalidOperationException($"The palette format '{paletteFormat}' is not supported by the plugin.");
        }

        private bool IsColorEncoding(int imageFormat)
        {
            return _imageState.SupportedEncodings?.ContainsKey(imageFormat) ?? false;
        }

        private bool IsIndexEncoding(int imageFormat)
        {
            return _imageState.SupportedIndexEncodings?.ContainsKey(imageFormat) ?? false;
        }

        private bool IsPointInRegion(Point point, Size region)
        {
            var rectangle = new Rectangle(Point.Empty, region);
            return rectangle.Contains(point);
        }
    }
}
