using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas.Configuration;

namespace Kontract.Models.Images
{
    /// <summary>
    /// The base bitmap info class.
    /// </summary>
    public class ImageInfo
    {
        /// <summary>
        /// The name of this image.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The data of this image.
        /// </summary>
        public byte[] ImageData { get; set; }

        /// <summary>
        /// The <see cref="Size"/> of this image.
        /// </summary>
        public Size ImageSize { get; set; }

        /// <summary>
        /// The format to use with this image.
        /// </summary>
        public int ImageFormat { get; set; }

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
    }
}
