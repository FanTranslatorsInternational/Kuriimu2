using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
            _bestImage = image;
            _decodedImage = null;
            _decodedPalette = null;

            var encodedImage = EncodeImage(image, _imageInfo.ImageFormat, _imageInfo.PaletteFormat, progress);
            _imageInfo.ImageData = encodedImage.imageData.FirstOrDefault();
            _imageInfo.MipMapData = encodedImage.imageData.Skip(1).ToArray();
            _imageInfo.ImageSize = image.Size;

            _imageInfo.ContentChanged = true;
        }

        /// <inheritdoc />
        public void SetPalette(IList<Color> palette, IProgressContext progress = null)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            _decodedImage = _bestImage = null;
            _decodedPalette = palette;

            _imageInfo.PaletteData = EncodePalette(palette, _imageInfo.PaletteFormat);

            _imageInfo.ContentChanged = true;
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
            var encodedImage = EncodeImage(image, _imageInfo.ImageFormat, _imageInfo.PaletteFormat);

            _imageInfo.ImageData = encodedImage.imageData.FirstOrDefault();
            _imageInfo.MipMapData = encodedImage.imageData.Skip(1).ToArray();
            _imageInfo.PaletteData = encodedImage.paletteData;

            _imageInfo.ContentChanged = true;
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
            _imageInfo.ImageData = imageData.FirstOrDefault();
            _imageInfo.MipMapData = imageData.Skip(1).ToArray();
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
            _imageInfo.ContentChanged = true;
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

            progress?.StartProgress();
            _decodedImage = decodeImageAction();
            progress?.FinishProgress();

            if (_bestImage == null)
                _bestImage = _decodedImage;

            return _decodedImage;
        }

        /// <summary>
        /// Decode current palette from <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Either buffered palette or decoded palette.</returns>
        private IList<Color> DecodePalette(IProgressContext context = null)
        {
            if (_decodedPalette != null)
                return _decodedPalette;

            return _decodedPalette = _imageState.SupportedPaletteEncodings[_imageInfo.PaletteFormat]
                .Load(_imageInfo.PaletteData, Environment.ProcessorCount)
                .ToArray();
        }

        /// <summary>
        /// Decode given palette data without buffering.
        /// </summary>
        /// <param name="paletteData">Palette data to decode.</param>
        /// <param name="context"></param>
        /// <returns>Decoded palette.</returns>
        private IList<Color> DecodePalette(byte[] paletteData, IProgressContext context = null)
        {
            return _imageState.SupportedPaletteEncodings[_imageInfo.PaletteFormat]
                .Load(paletteData, Environment.ProcessorCount)
                .ToArray();
        }

        private (IList<byte[]> imageData, byte[] paletteData) EncodeImage(Bitmap image, int imageFormat, int paletteFormat = -1,
            IProgressContext progress = null)
        {
            // Create transcoder
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

            progress?.StartProgress();
            var (mainImageData, mainPaletteData) = transcoder.Encode(image, progress);
            progress?.FinishProgress();

            var imageData = new byte[_imageInfo.MipMapCount + 1][];
            imageData[0] = mainImageData;

            // Decode palette if present, only when mip maps are needed
            IList<Color> decodedPalette = null;
            if (mainPaletteData != null && _imageInfo.MipMapCount > 0)
                decodedPalette = DecodePalette(mainPaletteData);

            // Encode mip maps
            var (width, height) = (image.Width / 2, image.Height / 2);
            for (var i = 0; i < _imageInfo.MipMapCount; i++)
            {
                imageData[i + 1] = EncodeMipMap(ResizeImage(image, width, height), imageFormat, decodedPalette);

                width /= 2;
                height /= 2;
            }

            return (imageData, mainPaletteData);
        }

        private byte[] EncodeMipMap(Bitmap mipMap, int imageFormat, IList<Color> palette = null)
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
                    .ConfigureQuantization(options => options.WithColorCount(indexEncoding.MaxColors).WithPalette(() => palette))
                    .TranscodeWith(size => indexEncoding)
                    .Build();
            }

            return transcoder.Encode(mipMap).imageData;
        }

        private Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using var wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }

            return destImage;
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
