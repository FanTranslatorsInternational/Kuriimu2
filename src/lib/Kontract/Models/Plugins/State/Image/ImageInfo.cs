using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using Kontract.Interfaces.Progress;
using Kontract.Kanvas.Interfaces;
using Kontract.Kanvas.Models;

namespace Kontract.Models.Plugins.State.Image
{
    /// <summary>
    /// The class to wrap an image plugin of the Kuriimu2 runtime to work with its actual image.
    /// Is not necessary for the Kanvas library itself to function properly.
    /// </summary>
    public abstract class ImageInfo : IImageInfo
    {
        private Bitmap _decodedImage;
        private Bitmap _bestImage;
        private IList<Color> _decodedPalette;

        #region Properties

        private int TaskCount => Environment.ProcessorCount;

        /// <inheritdoc />
        public int BitDepth => EncodingDefinition.ContainsColorEncoding(ImageFormat) ?
            EncodingDefinition.GetColorEncoding(ImageFormat).BitDepth :
            EncodingDefinition.GetIndexEncoding(ImageFormat).IndexEncoding.BitDepth;

        /// <inheritdoc />
        public EncodingDefinition EncodingDefinition { get; }

        /// <inheritdoc />
        public ImageData ImageData { get; }

        /// <inheritdoc />
        public bool IsIndexed => IsIndexEncoding(ImageFormat);

        /// <inheritdoc />
        public int ImageFormat => ImageData.Format;

        /// <inheritdoc />
        public int PaletteFormat => ImageData.PaletteFormat;

        /// <inheritdoc />
        public Size ImageSize => ImageData.ImageSize;

        /// <inheritdoc />
        public string Name => ImageData.Name;

        /// <inheritdoc />
        public bool IsImageLocked { get; }

        /// <inheritdoc />
        public bool ContentChanged => ImageData.ContentChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="encodingDefinition">The encoding definition for the image info.</param>
        /// <param name="imageData">The image info to represent.</param>
        public ImageInfo(EncodingDefinition encodingDefinition, ImageData imageData)
        {
            ContractAssertions.IsNotNull(encodingDefinition, nameof(encodingDefinition));
            ContractAssertions.IsNotNull(imageData, nameof(imageData));

            if (!encodingDefinition.Supports(imageData, out var error))
                throw new InvalidOperationException(error);

            EncodingDefinition = encodingDefinition;
            ImageData = imageData;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="encodingDefinition">The encoding definition for the image info.</param>
        /// <param name="imageData">The image info to represent.</param>
        /// <param name="lockImage">Locks the image to its initial dimension and encodings. This will throw an exception in the methods that may try such changes.</param>
        public ImageInfo(EncodingDefinition encodingDefinition, ImageData imageData, bool lockImage) :
            this(encodingDefinition, imageData)
        {
            IsImageLocked = lockImage;
        }

        #endregion

        protected abstract Bitmap GetDecodedImage(IProgressContext progress);
        protected abstract (byte[], byte[]) GetEncodedImage(Bitmap image, int imageFormat, int paletteFormat, IProgressContext progress);
        protected abstract byte[] GetEncodedMipMap(Bitmap image, int imageFormat, IList<Color> palette, IProgressContext progress);

        #region Image methods

        /// <inheritdoc />
        public Bitmap GetImage(IProgressContext progress = null)
        {
            return DecodeImage(progress);
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

            ImageData.Data = imageData.FirstOrDefault();
            ImageData.PaletteData = paletteData;
            ImageData.MipMapData = imageData.Skip(1).ToArray();
            ImageData.ImageSize = image.Size;

            ImageData.ContentChanged = true;
        }

        /// <inheritdoc />
        public void TranscodeImage(int imageFormat, IProgressContext progress = null)
        {
            if (IsImageLocked)
                throw new InvalidOperationException("Image cannot be transcoded to another format.");

            var paletteFormat = PaletteFormat;
            if (!IsIndexed && IsIndexEncoding(imageFormat))
                paletteFormat = EncodingDefinition.GetIndexEncoding(imageFormat).PaletteEncodingIndices.First();

            TranscodeInternal(imageFormat, paletteFormat, true, progress);
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
            var (imageData, paletteData) = EncodeImage(image, ImageData.Format, ImageData.PaletteFormat);

            ImageData.Data = imageData.FirstOrDefault();
            ImageData.MipMapData = imageData.Skip(1).ToArray();
            ImageData.PaletteData = paletteData;

            ImageData.ContentChanged = true;
        }

        #endregion

        #region Palette methods

        /// <inheritdoc />
        public IList<Color> GetPalette(IProgressContext progress = null)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            return DecodePalette(progress);
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

            ImageData.PaletteData = EncodePalette(palette, PaletteFormat);

            ImageData.ContentChanged = true;
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

        #endregion

        #region Decode methods

        private Bitmap DecodeImage(IProgressContext progress = null)
        {
            if (_decodedImage != null)
                return _decodedImage;

            ExecuteActionWithProgress(() => _decodedImage = GetDecodedImage(progress), progress);

            _bestImage ??= _decodedImage;

            return _decodedImage;
        }

        /// <summary>
        /// Decode current palette from <see cref="ImageData"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Either buffered palette or decoded palette.</returns>
        private IList<Color> DecodePalette(IProgressContext context = null)
        {
            if (_decodedPalette != null)
                return _decodedPalette;

            return _decodedPalette = DecodePalette(ImageData.PaletteData, context);
        }

        /// <summary>
        /// Decode given palette data without buffering.
        /// </summary>
        /// <param name="paletteData">Palette data to decode.</param>
        /// <param name="context"></param>
        /// <returns>Decoded palette.</returns>
        private IList<Color> DecodePalette(byte[] paletteData, IProgressContext context = null)
        {
            var paletteEncoding = EncodingDefinition.GetPaletteEncoding(PaletteFormat);
            return paletteEncoding
                .Load(paletteData, new EncodingLoadContext(new Size(1, paletteData.Length * 8 / paletteEncoding.BitsPerValue), TaskCount))
                .ToArray();
        }

        #endregion

        #region Encode methods

        private (IList<byte[]> imageData, byte[] paletteData) EncodeImage(Bitmap image, int imageFormat, int paletteFormat = -1, IProgressContext progress = null)
        {
            // Transcode image
            byte[] mainImageData = null;
            byte[] mainPaletteData = null;
            ExecuteActionWithProgress(() => (mainImageData, mainPaletteData) = GetEncodedImage(image, imageFormat, paletteFormat, progress), progress);

            var imageData = new byte[ImageData.MipMapCount + 1][];
            imageData[0] = mainImageData;

            // Decode palette if present, only when mip maps are needed
            IList<Color> decodedPalette = null;
            if (mainPaletteData != null && ImageData.MipMapCount > 0)
                decodedPalette = DecodePalette(mainPaletteData);

            // Encode mip maps
            var (width, height) = (image.Width / 2, image.Height / 2);
            for (var i = 0; i < ImageData.MipMapCount; i++)
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
            return GetEncodedMipMap(mipMap, imageFormat, palette, null);
        }

        private byte[] EncodePalette(IList<Color> palette, int paletteFormat)
        {
            return EncodingDefinition.GetPaletteEncoding(paletteFormat)
                .Save(palette, new EncodingSaveContext(new Size(1, palette.Count), TaskCount));
        }

        #endregion

        private void TranscodeInternal(int imageFormat, int paletteFormat, bool checkFormatEquality, IProgressContext progress = null)
        {
            AssertImageFormatExists(imageFormat);
            if (IsIndexEncoding(imageFormat))
                AssertPaletteFormatExists(paletteFormat);

            if (checkFormatEquality)
                if (ImageFormat == imageFormat &&
                    IsIndexEncoding(imageFormat) && PaletteFormat == paletteFormat)
                    return;

            // Decode image
            var decodedImage = _bestImage ?? DecodeImage(progress);

            // Update format information
            ImageData.Format = imageFormat;
            ImageData.PaletteFormat = IsIndexEncoding(imageFormat) ? paletteFormat : -1;

            // Encode image
            var (imageData, paletteData) = EncodeImage(decodedImage, imageFormat, paletteFormat, progress);

            // Set remaining image info properties
            ImageData.Data = imageData.FirstOrDefault();
            ImageData.MipMapData = imageData.Skip(1).ToArray();
            ImageData.PaletteData = IsIndexEncoding(imageFormat) ? paletteData : null;

            ImageData.ImageSize = decodedImage.Size;

            _decodedImage = null;
            _decodedPalette = null;

            ImageData.ContentChanged = true;
        }

        private Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
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

        private void AssertImageFormatExists(int imageFormat)
        {
            if (EncodingDefinition.GetColorEncoding(imageFormat) == null &&
                EncodingDefinition.GetIndexEncoding(imageFormat) == null)
                throw new InvalidOperationException($"The image format '{imageFormat}' is not supported by the plugin.");
        }

        private void AssertPaletteFormatExists(int paletteFormat)
        {
            if (EncodingDefinition.GetPaletteEncoding(paletteFormat) == null)
                throw new InvalidOperationException($"The palette format '{paletteFormat}' is not supported by the plugin.");
        }

        protected bool IsIndexEncoding(int imageFormat)
        {
            return EncodingDefinition.GetIndexEncoding(imageFormat) != null;
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
