using System;
using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Model;

namespace Kontract.Models.Image
{
    /// <summary>
    /// The base bitmap info class.
    /// </summary>
    public class ImageInfo
    {
        /// <summary>
        /// If the value image info contains palette information.
        /// </summary>
        public virtual bool HasPaletteInformation => PaletteData != null && PaletteFormat >= 0;

        /// <summary>
        /// The name of this image.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The data of this image.
        /// </summary>
        public byte[] ImageData { get; set; }

        /// <summary>
        /// The format to use with this image.
        /// </summary>
        public int ImageFormat { get; set; } = -1;

        /// <summary>
        /// The <see cref="Size"/> of this image.
        /// </summary>
        public Size ImageSize { get; set; }

        /// <summary>
        /// The palette data of the main image.
        /// </summary>
        public byte[] PaletteData { get; set; }

        /// <summary>
        /// The format in which the palette data is encoded.
        /// </summary>
        public int PaletteFormat { get; set; } = -1;

        /// <summary>
        /// The mip map data for this image.
        /// </summary>
        public IList<byte[]> MipMapData { get; set; }

        /// <summary>
        /// The count of mip maps for this image.
        /// </summary>
        public int MipMapCount => MipMapData?.Count ?? 0;

        /// <summary>
        /// The configuration to define a padding of the image size.
        /// </summary>
        public PadSizeConfiguration PadSize { get; } = new PadSizeConfiguration();

        /// <summary>
        /// The configuration to define a remapping of the pixels in the image, also known as swizzling.
        /// </summary>
        public RemapPixelsConfiguration RemapPixels { get; } = new RemapPixelsConfiguration();

        // TODO: Make not settable
        // TODO: Use KanvasImage in Kontract
        /// <summary>
        /// Determines of the content of this instance changed.
        /// </summary>
        public bool ContentChanged { get; set; }

        /// <summary>
        /// Creates a new <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="imageData">The encoded data of the image.</param>
        /// <param name="imageFormat">The format identifier for the encoded data.</param>
        /// <param name="imageSize">The size of the decoded image.</param>
        public ImageInfo(byte[] imageData, int imageFormat, Size imageSize)
        {
            ContractAssertions.IsNotNull(imageData, nameof(imageData));

            ImageData = imageData;
            ImageFormat = imageFormat;
            ImageSize = imageSize;
        }

        /// <summary>
        /// Creates a new <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="imageData">The encoded data of the image.</param>
        /// <param name="mipMaps">The encoded data of mip maps.</param>
        /// <param name="imageFormat">The format identifier for the encoded data.</param>
        /// <param name="imageSize">The size of the decoded image.</param>
        public ImageInfo(byte[] imageData, IList<byte[]> mipMaps, int imageFormat, Size imageSize) :
            this(imageData, imageFormat, imageSize)
        {
            ContractAssertions.IsNotNull(mipMaps, nameof(mipMaps));

            MipMapData = mipMaps;
        }
    }

    public class RemapPixelsConfiguration
    {
        private CreatePixelRemapper _func;

        public bool IsSet => _func != null;

        public void With(CreatePixelRemapper func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _func = func;
        }

        public IImageSwizzle Build(SwizzlePreparationContext context)
        {
            ContractAssertions.IsNotNull(_func, nameof(_func));

            return _func.Invoke(context);
        }
    }

    public class PadSizeConfiguration
    {
        private CreatePaddedSize _func;

        public bool IsSet => _func != null;

        public void With(CreatePaddedSize func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _func = func;
        }

        public void ToPowerOfTwo()
        {
            _func = size => new Size(ToPowerOfTwo(size.Width), ToPowerOfTwo(size.Height));
        }

        public void ToMultiple(int multiple)
        {
            _func = size => new Size(ToMultiple(size.Width, multiple), ToMultiple(size.Height, multiple));
        }

        public Size Build(Size size)
        {
            ContractAssertions.IsNotNull(_func, nameof(_func));

            return _func.Invoke(size);
        }

        private int ToPowerOfTwo(int value)
        {
            return 2 << (int)Math.Log(value - 1, 2);
        }

        private int ToMultiple(int value, int multiple)
        {
            return (value + (multiple - 1)) / multiple * multiple;
        }
    }
}
