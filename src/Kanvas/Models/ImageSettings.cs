using Kanvas.Interface;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Models
{
    /// <summary>
    /// Defines the settings with which an image will be loaded and saved.
    /// </summary>
    public class ImageSettings
    {
        /// <summary>
        /// Width of the image.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of the image
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The <see cref="IImageFormat"/> to load or save a color collection with.
        /// </summary>
        public IImageFormat Format { get; set; }

        /// <summary>
        /// The multiplicator the width is padded to.
        /// </summary>
        /// <remarks>0, if no padding is used.</remarks>
        public int PadWidth { get; set; } = 0;

        /// <summary>
        /// The multiplicator the height is padded to.
        /// </summary>
        /// <remarks>0, if no padding is used.</remarks>
        public int PadHeight { get; set; } = 0;

        /// <summary>
        /// The <see cref="IImageSwizzle"/> to swizzle the points in an image.
        /// </summary>
        public IImageSwizzle Swizzle { get; set; }

        /// <summary>
        /// A pixel shader applied on load and save.
        /// </summary>
        /// <remarks>This should only be used if every color in the collection is expected to be independantly changed by some common scheme.</remarks>
        /// <remarks>This property is not used for Palette operations</remarks>
        public Func<Color, Color> PixelShader { get; set; }

        [Obsolete]
        public ImageSettings()
        {

        }

        public ImageSettings(IImageFormat format, int width, int height)
        {
            Format = format;
            Width = width;
            Height = height;
        }
    }
}
