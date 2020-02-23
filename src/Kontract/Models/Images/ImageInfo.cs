using System;
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
        public string Name { get; set; }

        public Bitmap Image { get; set; }

        public Size ImageSize { get; set; }

        public int ImageFormat { get; set; }

        public IList<Bitmap> MipMapData { get; set; }

        public virtual int MipMapCount => MipMapData?.Count ?? 0;

        public IImageConfiguration Configuration { get; }

        public ImageInfo(Bitmap image, Size imageSize, int imageFormat, IImageConfiguration configuration)
        {
            ContractAssertions.IsNotNull(image, nameof(image));
            ContractAssertions.IsNotNull(configuration, nameof(configuration));
            if (imageSize == Size.Empty)
                throw new InvalidOperationException("Size has to be set for an image.");

            Configuration = configuration;
            ImageSize = imageSize;
            ImageFormat = imageFormat;
        }
    }
}
