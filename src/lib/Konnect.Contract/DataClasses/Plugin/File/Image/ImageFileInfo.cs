using Kanvas.Contract.Configuration;
using Kanvas.Contract.Enums;
using SixLabors.ImageSharp;

namespace Konnect.Contract.DataClasses.Plugin.File.Image;

/// <summary>
/// The base bitmap info class.
/// </summary>
public class ImageFileInfo
{
    /// <summary>
    /// The name of this image.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The bits per pixel used in the data.
    /// </summary>
    public required int BitDepth { get; set; }

    /// <summary>
    /// The bits per pixel used in the palette data.
    /// </summary>
    public int PaletteBitDepth { get; set; } = -1;

    /// <summary>
    /// The <see cref="Size"/> of this image.
    /// </summary>
    public required Size ImageSize { get; set; }

    /// <summary>
    /// The data of this image.
    /// </summary>
    public required byte[] ImageData { get; set; }

    /// <summary>
    /// The format to use with this image.
    /// </summary>
    public required int ImageFormat { get; set; }

    /// <summary>
    /// The palette data of the main image.
    /// </summary>
    public byte[]? PaletteData { get; set; }

    /// <summary>
    /// The format in which the palette data is encoded.
    /// </summary>
    public int PaletteFormat { get; set; } = -1;

    /// <summary>
    /// The mip map data for this image.
    /// </summary>
    public IList<byte[]>? MipMapData { get; set; }

    /// <summary>
    /// Defines where the image with its real size is anchored in the padded size.
    /// </summary>
    public ImageAnchor IsAnchoredAt { get; set; } = ImageAnchor.TopLeft;

    /// <summary>
    /// The configuration to define a padding of the image size.
    /// </summary>
    public CreatePaddedSizeDelegate? PadSize { get; set; } = null;

    /// <summary>
    /// The delegate to define a remapping of the pixels in the image, also known as swizzling.
    /// </summary>
    public CreatePixelRemapperDelegate? RemapPixels { get; set; } = null;

    /// <summary>
    /// The delegate to define the quantization configuration for the image.
    /// </summary>
    public CreateQuantizationDelegate? Quantize { get; set; } = null;

    /// <summary>
    /// Determines of the content of this instance changed.
    /// </summary>
    public bool ContentChanged { get; set; }
}