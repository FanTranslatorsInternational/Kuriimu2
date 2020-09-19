using System;
using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas.Configuration;

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
        /// The <see cref="IImageConfiguration"/> to encode or decode the image data.
        /// </summary>
        public IImageConfiguration Configuration { get; set; }

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
        public ImageInfo(byte[] imageData, IList<byte[]> mipMaps, int imageFormat, Size imageSize):
            this(imageData, imageFormat, imageSize)
        {
            ContractAssertions.IsNotNull(mipMaps,nameof(mipMaps));

            MipMapData = mipMaps;
        }
    }
}
