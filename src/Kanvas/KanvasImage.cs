using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using Kanvas.Configuration;
using Kontract;
using Kontract.Interfaces.Progress;
using Kontract.Models.Image;
using Kontract.Kanvas;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Model;

namespace Kanvas
{
    /// <summary>
    /// The class to wrap an image plugin of the Kuriimu2 runtime to work with its actual image.
    /// Is not necessary for the Kanvas library itself to function properly.
    /// </summary>
    public sealed class KanvasImage : IKanvasImage
    {
        private readonly EncodingDefinition _encodingDefinition;
        private Bitmap _decodedImage;
        private Bitmap _bestImage;
        private IList<Color> _decodedPalette;

        private int TaskCount => Environment.ProcessorCount;

        private IImageConfiguration ImageConfiguration =>
            ImageInfo.Configuration ?? new ImageConfiguration();

        /// <inheritdoc />
        public int BitDepth => _encodingDefinition.ContainsColorEncoding(ImageFormat) ?
            _encodingDefinition.GetColorEncoding(ImageFormat).BitDepth :
            _encodingDefinition.GetIndexEncoding(ImageFormat).IndexEncoding.BitDepth;

        /// <inheritdoc />
        public ImageInfo ImageInfo { get; }

        /// <inheritdoc />
        public bool IsIndexed => IsIndexEncoding(ImageFormat);

        /// <inheritdoc />
        public int ImageFormat => ImageInfo.ImageFormat;

        /// <inheritdoc />
        public int PaletteFormat => ImageInfo.PaletteFormat;

        /// <inheritdoc />
        public Size ImageSize => ImageInfo.ImageSize;

        /// <inheritdoc />
        public string Name => ImageInfo.Name;

        /// <inheritdoc />
        public bool IsImageLocked { get; }

        /// <summary>
        /// Creates a new instance of <see cref="KanvasImage"/>.
        /// </summary>
        /// <param name="encodingDefinition">The encoding definition for the image info.</param>
        /// <param name="imageInfo">The image info to represent.</param>
        public KanvasImage(EncodingDefinition encodingDefinition, ImageInfo imageInfo)
        {
            ContractAssertions.IsNotNull(encodingDefinition, nameof(encodingDefinition));
            ContractAssertions.IsNotNull(imageInfo, nameof(imageInfo));

            if (!encodingDefinition.Supports(imageInfo))
                throw new InvalidOperationException("The encoding definition can not support the image info.");

            _encodingDefinition = encodingDefinition;
            ImageInfo = imageInfo;
        }

        /// <summary>
        /// Creates a new instance of <see cref="KanvasImage"/>.
        /// </summary>
        /// <param name="encodingDefinition">The encoding definition for the image info.</param>
        /// <param name="imageInfo">The image info to represent.</param>
        /// <param name="lockImage">Locks the image to its initial dimension and encodings. This will throw an exception in the methods that may try such changes.</param>
        public KanvasImage(EncodingDefinition encodingDefinition, ImageInfo imageInfo, bool lockImage)
        {
            ContractAssertions.IsNotNull(encodingDefinition, nameof(encodingDefinition));
            ContractAssertions.IsNotNull(imageInfo, nameof(imageInfo));

            if (!encodingDefinition.Supports(imageInfo))
                throw new InvalidOperationException("The encoding definition can not support the image info.");

            _encodingDefinition = encodingDefinition;
            ImageInfo = imageInfo;

            IsImageLocked = lockImage;
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
            // Check for locking
            if (IsImageLocked && (ImageSize.Width != image.Width || ImageSize.Height != image.Height))
                throw new InvalidOperationException("Only images with the same dimensions can be set.");

            _bestImage = image;

            _decodedImage = null;
            _decodedPalette = null;

            var (imageData, paletteData) = EncodeImage(image, ImageFormat, PaletteFormat, progress);

            ImageInfo.ImageData = imageData.FirstOrDefault();
            ImageInfo.PaletteData = paletteData;
            ImageInfo.MipMapData = imageData.Skip(1).ToArray();
            ImageInfo.ImageSize = image.Size;

            ImageInfo.ContentChanged = true;
        }

        /// <inheritdoc />
        public void SetPalette(IList<Color> palette, IProgressContext progress = null)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            // Check for locking
            if (IsImageLocked && GetPalette(progress).Count != palette.Count)
                throw new InvalidOperationException("Only palettes with the same amount of colors can be set.");

            _decodedImage = _bestImage = null;
            _decodedPalette = palette;

            ImageInfo.PaletteData = EncodePalette(palette, PaletteFormat);

            ImageInfo.ContentChanged = true;
        }

        /// <inheritdoc />
        public void TranscodeImage(int imageFormat, IProgressContext progress = null)
        {
            if (IsImageLocked)
                throw new InvalidOperationException("Image cannot be transcoded to another format.");

            var paletteFormat = PaletteFormat;
            if (!IsIndexed && IsIndexEncoding(imageFormat))
                paletteFormat = _encodingDefinition.GetIndexEncoding(imageFormat).PaletteEncodingIndices.First();

            TranscodeInternal(imageFormat, paletteFormat, true, progress);
        }

        /// <inheritdoc />
        public void TranscodePalette(int paletteFormat, IProgressContext progress = null)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            if (IsImageLocked)
                throw new InvalidOperationException("Palette cannot be transcoded to another format.");

            TranscodeInternal(ImageFormat, paletteFormat, true, progress);
        }

        /// <inheritdoc />
        public void SetColorInPalette(int paletteIndex, Color color)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            var palette = DecodePalette();
            if (paletteIndex >= palette.Count)
                throw new InvalidOperationException($"Palette index {paletteIndex} is out of range.");

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
                throw new InvalidOperationException($"Palette index {paletteIndex} is out of range.");

            image.SetPixel(point.X, point.Y, palette[paletteIndex]);

            _decodedImage = image;
            var (imageData, paletteData) = EncodeImage(image, ImageInfo.ImageFormat, ImageInfo.PaletteFormat);

            ImageInfo.ImageData = imageData.FirstOrDefault();
            ImageInfo.MipMapData = imageData.Skip(1).ToArray();
            ImageInfo.PaletteData = paletteData;

            ImageInfo.ContentChanged = true;
        }

        private void TranscodeInternal(int imageFormat, int paletteFormat, bool checkFormatEquality, IProgressContext progress = null)
        {
            AssertImageFormatExists(imageFormat);
            if (IsIndexEncoding(imageFormat))
                AssertPaletteFormatExists(paletteFormat);

            if (checkFormatEquality)
                if (ImageFormat == imageFormat &&
                    IsIndexEncoding(imageFormat) && PaletteFormat == paletteFormat)
                    return;

            var decodedImage = _bestImage ?? DecodeImage(progress);
            var (imageData, paletteData) = EncodeImage(decodedImage, imageFormat, paletteFormat, progress);

            // Set image info properties
            ImageInfo.ImageData = imageData.FirstOrDefault();
            ImageInfo.MipMapData = imageData.Skip(1).ToArray();
            ImageInfo.ImageFormat = imageFormat;
            ImageInfo.ImageSize = decodedImage.Size;

            if (IsIndexEncoding(imageFormat))
            {
                ImageInfo.PaletteFormat = paletteFormat;
                ImageInfo.PaletteData = paletteData;
            }
            else
            {
                ImageInfo.PaletteFormat = -1;
                ImageInfo.PaletteData = null;
            }

            _decodedImage = null;
            _decodedPalette = null;

            ImageInfo.ContentChanged = true;
        }

        private Bitmap DecodeImage(IProgressContext progress = null)
        {
            if (_decodedImage != null)
                return _decodedImage;

            Func<Bitmap> decodeImageAction;
            if (IsIndexed)
            {
                var transcoder = ImageConfiguration.Clone()
                    .Transcode.With(_encodingDefinition.GetIndexEncoding(ImageFormat).IndexEncoding)
                    .TranscodePalette.With(_encodingDefinition.GetPaletteEncoding(PaletteFormat))
                    .Build();

                decodeImageAction = () => transcoder.Decode(ImageInfo.ImageData, ImageInfo.PaletteData, ImageInfo.ImageSize, progress);
            }
            else
            {
                var transcoder = ImageConfiguration.Clone()
                    .Transcode.With(_encodingDefinition.GetColorEncoding(ImageFormat))
                    .Build();

                decodeImageAction = () => transcoder.Decode(ImageInfo.ImageData, ImageInfo.ImageSize, progress);
            }

            ExecuteActionWithProgress(() => _decodedImage = decodeImageAction(), progress);

            _bestImage ??= _decodedImage;

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

            return _decodedPalette = DecodePalette(ImageInfo.PaletteData, context);
        }

        /// <summary>
        /// Decode given palette data without buffering.
        /// </summary>
        /// <param name="paletteData">Palette data to decode.</param>
        /// <param name="context"></param>
        /// <returns>Decoded palette.</returns>
        private IList<Color> DecodePalette(byte[] paletteData, IProgressContext context = null)
        {
            return _encodingDefinition.GetPaletteEncoding(PaletteFormat)
                .Load(paletteData, new EncodingLoadContext(TaskCount))
                .ToArray();
        }

        private (IList<byte[]> imageData, byte[] paletteData) EncodeImage(Bitmap image, int imageFormat, int paletteFormat = -1,
            IProgressContext progress = null)
        {
            // Create transcoder
            IImageTranscoder transcoder;
            if (IsIndexEncoding(imageFormat))
            {
                var indexEncoding = _encodingDefinition.GetIndexEncoding(imageFormat).IndexEncoding;
                transcoder = ImageConfiguration.Clone()
                    .ConfigureQuantization(options => options.WithColorCount(indexEncoding.MaxColors))
                    .Transcode.With(indexEncoding)
                    .TranscodePalette.With(_encodingDefinition.GetPaletteEncoding(paletteFormat))
                    .Build();
            }
            else
            {
                transcoder = ImageConfiguration.Clone()
                    .Transcode.With(_encodingDefinition.GetColorEncoding(imageFormat))
                    .Build();
            }

            byte[] mainImageData = null;
            byte[] mainPaletteData = null;
            ExecuteActionWithProgress(() => (mainImageData, mainPaletteData) = transcoder.Encode(image, progress), progress);

            var imageData = new byte[ImageInfo.MipMapCount + 1][];
            imageData[0] = mainImageData;

            // Decode palette if present, only when mip maps are needed
            IList<Color> decodedPalette = null;
            if (mainPaletteData != null && ImageInfo.MipMapCount > 0)
                decodedPalette = DecodePalette(mainPaletteData);

            // Encode mip maps
            var (width, height) = (image.Width / 2, image.Height / 2);
            for (var i = 0; i < ImageInfo.MipMapCount; i++)
            {
                imageData[i + 1] = EncodeMipMap(ResizeImage(image, width, height), imageFormat, decodedPalette);

                width /= 2;
                height /= 2;
            }

            return (imageData, mainPaletteData);
        }

        // TODO: Use progress
        private byte[] EncodeMipMap(Bitmap mipMap, int imageFormat, IList<Color> palette = null)
        {
            IImageTranscoder transcoder;
            if (IsIndexEncoding(imageFormat))
            {
                var indexEncoding = _encodingDefinition.GetIndexEncoding(imageFormat).IndexEncoding;
                transcoder = ImageConfiguration.Clone()
                    .ConfigureQuantization(options => options.WithColorCount(indexEncoding.MaxColors).WithPalette(() => palette))
                    .Transcode.With(indexEncoding)
                    .Build();
            }
            else
            {
                transcoder = ImageConfiguration.Clone()
                    .Transcode.With(_encodingDefinition.GetColorEncoding(imageFormat))
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
            return _encodingDefinition.GetPaletteEncoding(paletteFormat)
                .Save(palette, new EncodingSaveContext(TaskCount));
        }

        private void AssertImageFormatExists(int imageFormat)
        {
            if (_encodingDefinition.GetColorEncoding(imageFormat) == null &&
               _encodingDefinition.GetIndexEncoding(imageFormat) == null)
                throw new InvalidOperationException($"The image format '{imageFormat}' is not supported by the plugin.");
        }

        private void AssertPaletteFormatExists(int paletteFormat)
        {
            if (_encodingDefinition.GetPaletteEncoding(paletteFormat) == null)
                throw new InvalidOperationException($"The palette format '{paletteFormat}' is not supported by the plugin.");
        }

        private bool IsIndexEncoding(int imageFormat)
        {
            return _encodingDefinition.GetIndexEncoding(imageFormat) != null;
        }

        private bool IsPointInRegion(Point point, Size region)
        {
            var rectangle = new Rectangle(Point.Empty, region);
            return rectangle.Contains(point);
        }

        private void ExecuteActionWithProgress(Action action, IProgressContext progress = null)
        {
            var isRunning = progress?.IsRunning();
            if (isRunning.HasValue && !isRunning.Value)
                progress.StartProgress();

            action();

            if (isRunning.HasValue && !isRunning.Value)
                progress.FinishProgress();
        }

        public void Dispose()
        {
            _decodedImage?.Dispose();
            _bestImage?.Dispose();
        }
    }
}
