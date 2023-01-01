using System;
using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas.Interfaces;
using Kontract.Kanvas.Interfaces.Configuration;
using Kontract.Kanvas.Models;

namespace Kontract.Models.Plugins.State.Image
{
    /// <summary>
    /// The base bitmap info class.
    /// </summary>
    public class ImageData
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
        /// The <see cref="Size"/> of this image.
        /// </summary>
        public Size ImageSize { get; set; }

        /// <summary>
        /// The data of this image.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// The format to use with this image.
        /// </summary>
        public int Format { get; set; } = -1;

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

        /// <summary>
        /// Defines where the image with its real size is anchored in the padded size.
        /// </summary>
        public ImageAnchor IsAnchoredAt { get; set; } = ImageAnchor.TopLeft;

        // TODO: Make not settable
        // TODO: Use KanvasImage in Kontract
        /// <summary>
        /// Determines of the content of this instance changed.
        /// </summary>
        public bool ContentChanged { get; set; }

        /// <summary>
        /// Creates a new <see cref="ImageData"/>.
        /// </summary>
        /// <param name="data">The encoded data of the image.</param>
        /// <param name="format">The format identifier for the encoded data.</param>
        /// <param name="imageSize">The size of the decoded image.</param>
        public ImageData(byte[] data, int format, Size imageSize)
        {
            ContractAssertions.IsNotNull(data, nameof(data));

            Data = data;
            Format = format;
            ImageSize = imageSize;
        }

        /// <summary>
        /// Creates a new <see cref="ImageData"/>.
        /// </summary>
        /// <param name="data">The encoded data of the image.</param>
        /// <param name="mipMaps">The encoded data of mip maps.</param>
        /// <param name="imageFormat">The format identifier for the encoded data.</param>
        /// <param name="imageSize">The size of the decoded image.</param>
        public ImageData(byte[] data, IList<byte[]> mipMaps, int imageFormat, Size imageSize) :
            this(data, imageFormat, imageSize)
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
        private readonly PadSizeDimensionConfiguration _widthConfig;
        private readonly PadSizeDimensionConfiguration _heightConfig;

        public PadSizeDimensionConfiguration Width => _widthConfig;
        public PadSizeDimensionConfiguration Height => _heightConfig;

        public PadSizeConfiguration()
        {
            _widthConfig = new PadSizeDimensionConfiguration(this);
            _heightConfig = new PadSizeDimensionConfiguration(this);
        }

        public void ToPowerOfTwo(int steps = 1)
        {
            Width.ToPowerOfTwo(steps);
            Height.ToPowerOfTwo(steps);
        }

        public void ToMultiple(int multiple)
        {
            Width.ToMultiple(multiple);
            Height.ToMultiple(multiple);
        }

        public Size Build(Size imageSize)
        {
            var width = _widthConfig.Delegate?.Invoke(imageSize.Width) ?? imageSize.Width;
            var height = _heightConfig.Delegate?.Invoke(imageSize.Height) ?? imageSize.Height;

            return new Size(width, height);
        }
    }

    public class PadSizeDimensionConfiguration
    {
        private readonly PadSizeConfiguration _parent;

        internal CreatePaddedSizeDimension Delegate { get; private set; }

        public PadSizeDimensionConfiguration(PadSizeConfiguration parent)
        {
            _parent = parent;
        }

        public PadSizeConfiguration To(CreatePaddedSizeDimension func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            Delegate = func;

            return _parent;
        }

        public PadSizeConfiguration ToPowerOfTwo(int steps = 1)
        {
            int ToPowerOfTwoInternal(int value) => 2 << (int)Math.Log(value - 1, 2);

            Delegate = value => ToPowerOfTwoInternal(value) << (steps - 1);

            return _parent;
        }

        public PadSizeConfiguration ToMultiple(int multiple)
        {
            Delegate = i => ToMultiple(i, multiple);

            return _parent;
        }

        private int ToMultiple(int value, int multiple)
        {
            return (value + (multiple - 1)) / multiple * multiple;
        }
    }
}
