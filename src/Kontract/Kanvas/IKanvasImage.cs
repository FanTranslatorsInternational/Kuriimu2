using System;
using System.Collections.Generic;
using System.Drawing;
using Kontract.Interfaces.Progress;
using Kontract.Models.Image;

namespace Kontract.Kanvas
{
    /// <summary>
    /// Exposes properties and methods to retrieve and manipulate an image given by an image plugin;
    /// </summary>
    public interface IKanvasImage : IDisposable
    {
        /// <summary>
        /// The bit depth of the current image encoding used.
        /// </summary>
        int BitDepth { get; }

        /// <summary>
        /// The image information provided by an image plugin.
        /// </summary>
        /// <remarks>This instance may not be changed manually.</remarks>
        ImageInfo ImageInfo { get; }

        /// <summary>
        /// If the image is encoded with an <see cref="IIndexEncoding"/>.
        /// </summary>
        bool IsIndexed { get; }

        /// <summary>
        /// The current format the image is encoded in.
        /// </summary>
        int ImageFormat { get; }

        /// <summary>
        /// The current format the palette is encoded in.
        /// </summary>
        int PaletteFormat { get; }

        /// <summary>
        /// The current size of the image.
        /// </summary>
        Size ImageSize { get; }

        /// <summary>
        /// The name of the image.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Decides if the image is locked to its current dimensions and encodings.
        /// </summary>
        bool IsImageLocked { get; }

        /// <summary>
        /// Gets the image of the set <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="progress">The progress for this action.</param>
        /// <returns>The decoded image.</returns>
        Bitmap GetImage(IProgressContext progress = null);

        /// <summary>
        /// Sets the image of the set <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="image">The image to encode and set.</param>
        /// <param name="progress">The progress for this action.</param>
        void SetImage(Bitmap image, IProgressContext progress = null);

        /// <summary>
        /// Change the image's color encoding.
        /// </summary>
        /// <param name="imageFormat">The new image format.</param>
        /// <param name="progress">The progress for this action.</param>
        void TranscodeImage(int imageFormat, IProgressContext progress = null);

        /// <summary>
        /// Gets the palette of the set <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="progress">The progress for this action.</param>
        /// <returns>The decoded palette.</returns>
        /// <remarks>Throws if the image is encoded with an <see cref="IColorIndexEncoding"/>.</remarks>
        IList<Color> GetPalette(IProgressContext progress = null);

        /// <summary>
        /// Sets the palette of the set <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="palette">The palette to encode and set.</param>
        /// <param name="progress">The progress for this action.</param>
        /// <remarks>Throws if the image is encoded with an <see cref="IColorIndexEncoding"/>.</remarks>
        void SetPalette(IList<Color> palette, IProgressContext progress = null);

        /// <summary>
        /// Change the palette's color encoding.
        /// </summary>
        /// <param name="paletteFormat">The new palette format.</param>
        /// <param name="progress">The progress for this action.</param>
        /// <remarks>Throws if the image is encoded with an <see cref="IColorIndexEncoding"/>.</remarks>
        void TranscodePalette(int paletteFormat, IProgressContext progress = null);

        /// <summary>
        /// Sets a color at any index in the palette. 
        /// </summary>
        /// <param name="paletteIndex">The index into the palette.</param>
        /// <param name="color">The new color at the given index.</param>
        /// <remarks>Throws if the image is encoded with an <see cref="IColorIndexEncoding"/>.</remarks>
        void SetColorInPalette(int paletteIndex, Color color);

        /// <summary>
        /// Sets a palette index at any position in the image.
        /// </summary>
        /// <param name="point">The position to set the index at.</param>
        /// <param name="paletteIndex">The index to set.</param>
        /// <remarks>Throws if the image is encoded with an <see cref="IColorIndexEncoding"/>.</remarks>
        void SetIndexInImage(Point point, int paletteIndex);
    }
}
