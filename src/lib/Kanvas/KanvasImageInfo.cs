using System.Collections.Generic;
using System.Drawing;
using Kanvas.Configuration;
using Kontract.Interfaces.Progress;
using Kontract.Kanvas.Interfaces;
using Kontract.Models.Plugins.State.Image;

namespace Kanvas
{
    /// <summary>
    /// The class to wrap an image plugin of the Kuriimu2 runtime to work with its actual image.
    /// Is not necessary for the Kanvas library itself to function properly.
    /// </summary>
    public sealed class KanvasImageInfo : ImageInfo
    {
        public KanvasImageInfo(EncodingDefinition encodingDefinition, ImageData imageData) : base(encodingDefinition, imageData)
        {
        }

        public KanvasImageInfo(EncodingDefinition encodingDefinition, ImageData imageData, bool lockImage) : base(encodingDefinition, imageData, lockImage)
        {
        }

        protected override Bitmap GetDecodedImage(IProgressContext progress)
        {
            IImageTranscoder transcoder;

            if (IsIndexed)
            {
                transcoder = CreateImageConfiguration(ImageFormat, PaletteFormat)
                    .Transcode.With(EncodingDefinition.GetIndexEncoding(ImageFormat).IndexEncoding)
                    .TranscodePalette.With(EncodingDefinition.GetPaletteEncoding(PaletteFormat))
                    .Build();

                return transcoder.Decode(ImageData.Data, ImageData.PaletteData, ImageData.ImageSize, progress);
            }

            transcoder = CreateImageConfiguration(ImageFormat, PaletteFormat)
                .Transcode.With(EncodingDefinition.GetColorEncoding(ImageFormat))
                .Build();

            return transcoder.Decode(ImageData.Data, ImageData.ImageSize, progress);
        }

        protected override (byte[], byte[]) GetEncodedImage(Bitmap image, int imageFormat, int paletteFormat, IProgressContext progress)
        {
            IImageTranscoder transcoder;

            if (IsIndexEncoding(imageFormat))
            {
                var indexEncoding = EncodingDefinition.GetIndexEncoding(imageFormat).IndexEncoding;
                transcoder = CreateImageConfiguration(imageFormat, paletteFormat)
                    .ConfigureQuantization(options => options.WithColorCount(indexEncoding.MaxColors))
                    .Transcode.With(indexEncoding)
                    .TranscodePalette.With(EncodingDefinition.GetPaletteEncoding(imageFormat))
                    .Build();
            }
            else
            {
                transcoder = CreateImageConfiguration(imageFormat, paletteFormat)
                    .Transcode.With(EncodingDefinition.GetColorEncoding(imageFormat))
                    .Build();
            }

            return transcoder.Encode(image, progress);
        }

        protected override byte[] GetEncodedMipMap(Bitmap image, int imageFormat, IList<Color> palette, IProgressContext progress)
        {
            IImageTranscoder transcoder;

            if (IsIndexEncoding(imageFormat))
            {
                var indexEncoding = EncodingDefinition.GetIndexEncoding(imageFormat).IndexEncoding;
                transcoder = CreateImageConfiguration(ImageFormat, PaletteFormat)
                    .ConfigureQuantization(options => options.WithColorCount(indexEncoding.MaxColors).WithPalette(() => palette))
                    .Transcode.With(indexEncoding)
                    .Build();
            }
            else
            {
                transcoder = CreateImageConfiguration(ImageFormat, PaletteFormat)
                    .Transcode.With(EncodingDefinition.GetColorEncoding(imageFormat))
                    .Build();
            }

            return transcoder.Encode(image).imageData;
        }

        private ImageConfiguration CreateImageConfiguration(int imageFormat, int paletteFormat)
        {
            var config = new ImageConfiguration();

            config.PadSize.With(options => options.To(size => ImageData.PadSize.Build(size)));
            config.IsAnchoredAt(ImageData.IsAnchoredAt);

            if (ImageData.RemapPixels.IsSet)
                config.RemapPixels.With(context => ImageData.RemapPixels.Build(context));

            if (IsIndexEncoding(imageFormat) && EncodingDefinition.ContainsPaletteShader(paletteFormat))
                config.ShadeColors.With(() => EncodingDefinition.GetPaletteShader(paletteFormat));
            if (!IsIndexEncoding(imageFormat) && EncodingDefinition.ContainsColorShader(imageFormat))
                config.ShadeColors.With(() => EncodingDefinition.GetColorShader(imageFormat));

            return config;
        }
    }
}
