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
        public int Width { get; }

        /// <summary>
        /// Height of the image
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The <see cref="IColorEncoding"/> to load or save colors with.
        /// </summary>
        public IColorEncoding Encoding { get; }

        /// <summary>
        /// The multiplicand the width is padded to.
        /// </summary>
        /// <remarks>0, if no padding is used.</remarks>
        public int PadWidth { get; set; } = 0;

        /// <summary>
        /// The multiplicand the height is padded to.
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
        /// <remarks>This should only be used if every color in the collection is expected to be independently changed by some common scheme.</remarks>
        /// <remarks>This property is not used for Palette operations</remarks>
        public Func<Color, Color> PixelShader { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ImageSettings"/>.
        /// </summary>
        /// <param name="encoding">The encoding used for the colors in an image.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        public ImageSettings(IColorEncoding encoding, int width, int height)
        {
            Encoding = encoding;
            Width = width;
            Height = height;
        }
    }
}
