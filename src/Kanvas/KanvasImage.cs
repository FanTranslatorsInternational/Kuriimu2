using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Models.Image;

namespace Kanvas
{
    public interface IKanvasImage
    {
        bool IsIndexed { get; }

        Image GetImage();

        void SetImage(Image image);

        IReadOnlyList<Color> GetPalette();

        void SetPalette(IReadOnlyList<Color> palette);
    }

    /// <summary>
    /// The class to wrap an image plugin of the Kuriimu2 runtime to work with its actual image.
    /// Is not necessary for the Kanvas library itself to function properly.
    /// </summary>
    public class KanvasImage : IKanvasImage
    {
        private readonly IImageState _imageState;
        private readonly int _imageIndex;

        private Bitmap _decodedImage;
        private IList<Color> _decodedPalette;

        protected ImageInfo ImageInfo
        {
            get => _imageState.Images[_imageIndex];
            set => _imageState.Images[_imageIndex] = value;
        }

        public bool IsIndexed => IsIndexEncoding(ImageInfo.ImageFormat);

        public KanvasImage(IImageState imageState, ImageInfo imageInfo)
        {
            ContractAssertions.IsNotNull(imageState, nameof(imageState));
            ContractAssertions.IsNotNull(imageInfo, nameof(imageInfo));
            ContractAssertions.IsElementContained(imageState.Images, imageInfo, nameof(imageState.Images), nameof(imageInfo));

            _imageState = imageState;
            _imageIndex = imageState.Images.IndexOf(imageInfo);
        }

        public Bitmap GetImage(IProgressContext progress = null)
        {
            return DecodeImage(progress);
        }

        public void SetImage(Bitmap image, IProgressContext progress = null)
        {
            _decodedImage = image;
            TranscodeInternal(ImageInfo.ImageFormat, ImageInfo.PaletteFormat, false, progress);
        }

        public void Transcode(int imageFormat, IProgressContext progress = null)
        {
            TranscodeInternal(imageFormat, ImageInfo.PaletteFormat, true, progress);
        }

        public IReadOnlyList<Color> GetPalette(IProgressContext progress)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            return DecodePalette(progress);
        }

        public void SetPalette(IReadOnlyList<Color> palette)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));
            // TODO:
        }

        public void TranscodePalette(int paletteFormat, IProgressContext progress = null)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            TranscodeInternal(ImageInfo.ImageFormat, paletteFormat, true, progress);
        }

        private void TranscodeInternal(int imageFormat, int paletteFormat, bool checkFormatEquality, IProgressContext progress = null)
        {
            AssertImageFormatExists(imageFormat);
            if (IsIndexEncoding(imageFormat))
                AssertPaletteFormatExists(paletteFormat);

            if (checkFormatEquality)
                if (ImageInfo.ImageFormat == imageFormat ||
                    IsIndexEncoding(imageFormat) && ImageInfo.PaletteFormat == paletteFormat)
                    return;

            var decodedImage = DecodeImage(progress);
            var (imageData, paletteData) = EncodeImage(decodedImage, imageFormat, paletteFormat, progress);

            // Set image info properties
            ImageInfo.ImageFormat = imageFormat;
            ImageInfo.ImageData = imageData;
            ImageInfo.ImageSize = decodedImage.Size;

            if (IsIndexEncoding(imageFormat))
            {
                ImageInfo.PaletteFormat = paletteFormat;
                ImageInfo.PaletteData = paletteData;
            }
        }

        private Bitmap DecodeImage(IProgressContext progress = null)
        {
            if (_decodedImage != null)
                return _decodedImage;

            Func<Bitmap> decodeImageAction;
            if (IsIndexed)
            {
                var transcoder = ImageInfo.Configuration.Clone()
                    .TranscodeWith(size => _imageState.SupportedIndexEncodings[ImageInfo.ImageFormat].Encoding)
                    .TranscodePaletteWith(() => _imageState.SupportedPaletteEncodings[ImageInfo.PaletteFormat])
                    .Build();

                decodeImageAction = () => transcoder.Decode(ImageInfo.ImageData, ImageInfo.PaletteData, ImageInfo.ImageSize, progress);
            }
            else
            {
                var transcoder = ImageInfo.Configuration.Clone()
                    .TranscodeWith(size => _imageState.SupportedEncodings[ImageInfo.ImageFormat])
                    .Build();

                decodeImageAction = () => transcoder.Decode(ImageInfo.ImageData, ImageInfo.ImageSize, progress);
            }

            return _decodedImage = decodeImageAction();
        }

        private IList<Color> DecodePalette(IProgressContext context = null)
        {
            if (_decodedPalette != null)
                return _decodedPalette;

            return _decodedPalette = _imageState.SupportedPaletteEncodings[ImageInfo.PaletteFormat]
                .Load(ImageInfo.PaletteData, Environment.ProcessorCount)
                .ToArray();
        }

        private (byte[] imageData, byte[] paletteData) EncodeImage(Bitmap image, int imageFormat, int paletteFormat = -1,
            IProgressContext progress = null)
        {
            if (IsColorEncoding(imageFormat))
            {
                var transcoder = ImageInfo.Configuration.Clone()
                    .TranscodeWith(size => _imageState.SupportedEncodings[imageFormat])
                    .Build();

                return (transcoder.Encode(image, progress), null);
            }
            else
            {
                var indexEncoding = _imageState.SupportedIndexEncodings[imageFormat].Encoding;
                var transcoder = ImageInfo.Configuration.Clone()
                    .ConfigureQuantization(options => options.WithColorCount(indexEncoding.MaxColors))
                    .TranscodeWith(size => indexEncoding)
                    .TranscodePaletteWith(() => _imageState.SupportedPaletteEncodings[paletteFormat])
                    .Build();

                return transcoder.Encode(image, progress);
            }
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
    }
}
