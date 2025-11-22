using Kanvas.Contract.Encoding;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Progress;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Konnect.Contract.Plugin.File.Image;

public interface IImageFile
{
    /// <summary>
    /// The encoding definition for this image.
    /// </summary>
    IEncodingDefinition EncodingDefinition { get; }

    /// <summary>
    /// The image information provided by an image plugin.
    /// </summary>
    /// <remarks>This instance may not be changed manually.</remarks>
    ImageFileInfo ImageInfo { get; }

    /// <summary>
    /// If the image is encoded with an <see cref="IIndexEncoding"/>.
    /// </summary>
    bool IsIndexed { get; }

    /// <summary>
    /// Decides if the image is locked to its current dimensions and encodings.
    /// </summary>
    bool IsImageLocked { get; }

    /// <summary>
    /// Clones all data of this image to a new instance.
    /// </summary>
    /// <returns>The cloned <see cref="IImageFile"/>.</returns>
    IImageFile Clone();

    /// <summary>
    /// Gets the image of the set <see cref="ImageInfo"/>.
    /// </summary>
    /// <param name="progress">The progress for this action.</param>
    /// <returns>The decoded image.</returns>
    Image<Rgba32> GetImage(IProgressContext? progress = null);

    /// <summary>
    /// Sets the image of the set <see cref="ImageInfo"/>.
    /// </summary>
    /// <param name="image">The image to encode and set.</param>
    /// <param name="progress">The progress for this action.</param>
    void SetImage(Image<Rgba32> image, IProgressContext? progress = null);

    /// <summary>
    /// Change the image's color encoding.
    /// </summary>
    /// <param name="imageFormat">The new image format.</param>
    /// <param name="progress">The progress for this action.</param>
    void TranscodeImage(int imageFormat, IProgressContext? progress = null);

    /// <summary>
    /// Gets the palette of the set <see cref="ImageInfo"/>.
    /// </summary>
    /// <param name="progress">The progress for this action.</param>
    /// <returns>The decoded palette.</returns>
    /// <remarks>Throws if the image does not have a palette.</remarks>
    IList<Rgba32> GetPalette(IProgressContext? progress = null);

    /// <summary>
    /// Sets the palette of the set <see cref="ImageInfo"/>.
    /// </summary>
    /// <param name="palette">The palette to encode and set.</param>
    /// <param name="progress">The progress for this action.</param>
    /// <remarks>Throws if the image does not have a palette.</remarks>
    void SetPalette(IList<Rgba32> palette, IProgressContext? progress = null);

    /// <summary>
    /// Change the palette's color encoding.
    /// </summary>
    /// <param name="paletteFormat">The new palette format.</param>
    /// <param name="progress">The progress for this action.</param>
    /// <remarks>Throws if the image does not have a palette.</remarks>
    void TranscodePalette(int paletteFormat, IProgressContext? progress = null);

    /// <summary>
    /// Sets a color at any index in the palette. 
    /// </summary>
    /// <param name="paletteIndex">The index into the palette.</param>
    /// <param name="color">The new color at the given index.</param>
    /// <remarks>Throws if the image does not have a palette.</remarks>
    void SetColorInPalette(int paletteIndex, Rgba32 color);

    /// <summary>
    /// Sets a palette index at any position in the image.
    /// </summary>
    /// <param name="point">The position to set the index at.</param>
    /// <param name="paletteIndex">The index to set.</param>
    /// <remarks>Throws if the image does not have a palette.</remarks>
    void SetIndexInImage(Point point, int paletteIndex);
}