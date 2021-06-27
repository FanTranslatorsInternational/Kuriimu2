using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Kanvas.Encoding;
using Kontract;
using Kontract.Interfaces.Progress;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace Kanvas
{
    public class BitmapKanvasImage : IKanvasImage
    {
        private Bitmap _image;

        public int BitDepth { get; }
        public EncodingDefinition EncodingDefinition { get; }
        public ImageInfo ImageInfo => null;
        public bool IsIndexed => false;
        public int ImageFormat => 0;
        public int PaletteFormat => 0;
        public Size ImageSize => _image.Size;
        public string Name { get; }
        public bool IsImageLocked => true;
        public bool ContentChanged { get; set; }

        public BitmapKanvasImage(Bitmap image)
        {
            var encoding = GetImageEncoding(image);

            EncodingDefinition = new EncodingDefinition();
            EncodingDefinition.AddColorEncoding(0, encoding);

            BitDepth = encoding.BitDepth;

            _image = image;
        }

        public BitmapKanvasImage(Bitmap image, string name) : this(image)
        {
            Name = name;
        }

        public Bitmap GetImage(IProgressContext progress = null)
        {
            return _image;
        }

        public void SetImage(Bitmap image, IProgressContext progress = null)
        {
            ContentChanged = true;
            _image = image;
        }

        public void TranscodeImage(int imageFormat, IProgressContext progress = null)
        {
            if (IsImageLocked)
                throw new InvalidOperationException("Image cannot be transcoded to another format.");

            throw new InvalidOperationException("Transcoding image is not supported for bitmaps.");
        }

        public IList<Color> GetPalette(IProgressContext progress = null)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            throw new InvalidOperationException("Getting palette is not supported for bitmaps.");
        }

        public void SetPalette(IList<Color> palette, IProgressContext progress = null)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            throw new InvalidOperationException("Setting palette is not supported for bitmaps.");
        }

        public void TranscodePalette(int paletteFormat, IProgressContext progress = null)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            throw new InvalidOperationException("Transcoding palette is not supported for bitmaps.");
        }

        public void SetColorInPalette(int paletteIndex, Color color)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            throw new InvalidOperationException("Setting color in palette is not supported for bitmaps.");
        }

        public void SetIndexInImage(Point point, int paletteIndex)
        {
            ContractAssertions.IsTrue(IsIndexed, nameof(IsIndexed));

            throw new InvalidOperationException("Setting index in image is not supported for bitmaps.");
        }

        public void Dispose()
        {
            _image = null;
        }

        private IColorEncoding GetImageEncoding(Bitmap image)
        {
            switch (image.PixelFormat)
            {
                case PixelFormat.Format32bppArgb: return new Rgba(8, 8, 8, 8, "ARGB");
                case PixelFormat.Format16bppArgb1555: return new Rgba(5, 5, 5, 1, "ARGB");
                case PixelFormat.Format16bppGrayScale: return new La(16, 0);
                case PixelFormat.Format16bppRgb555: return new Rgba(5, 5, 5);
                case PixelFormat.Format16bppRgb565: return new Rgba(5, 6, 5);
                case PixelFormat.Format24bppRgb: return new Rgba(8, 8, 8);
                case PixelFormat.Format32bppRgb: return new Rgba(8, 8, 8, 8, "RGBX");
                default: throw new InvalidOperationException($"{image.PixelFormat} is not supported.");
            }
        }
    }
}
