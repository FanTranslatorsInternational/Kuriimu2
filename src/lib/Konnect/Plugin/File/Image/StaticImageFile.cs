using Kanvas;
using Kanvas.Contract.Encoding;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Contract.Progress;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Konnect.Plugin.File.Image;

public class StaticImageFile : IImageFile
{
    private static readonly EncodingDefinition _encodingDefinition;

    private Image<Rgba32> _image;

    public IEncodingDefinition EncodingDefinition => _encodingDefinition;
    public ImageFileInfo ImageInfo { get; }
    public bool IsIndexed => false;
    public bool IsImageLocked => true;

    static StaticImageFile()
    {
        IColorEncoding encoding = ImageFormats.Rgba8888();

        _encodingDefinition = new EncodingDefinition();
        _encodingDefinition.AddColorEncoding(0, encoding);
    }

    public StaticImageFile(Image<Rgba32> image)
    {
        _image = image;

        ImageInfo = new ImageFileInfo
        {
            BitDepth = 32,
            ImageSize = image.Size,
            ImageData = [],
            ImageFormat = 0
        };
    }

    public StaticImageFile(Image<Rgba32> image, string name) : this(image)
    {
        _image = image;

        ImageInfo = new ImageFileInfo
        {
            Name = name,
            BitDepth = 32,
            ImageSize = image.Size,
            ImageData = [],
            ImageFormat = 0
        };
    }

    public IImageFile Clone()
    {
        return !string.IsNullOrEmpty(ImageInfo.Name)
            ? new StaticImageFile(_image.Clone(), ImageInfo.Name)
            : new StaticImageFile(_image.Clone());
    }

    public Image<Rgba32> GetImage(IProgressContext? progress = null)
    {
        return _image;
    }

    public void SetImage(Image<Rgba32> image, IProgressContext? progress = null)
    {
        ImageInfo.ContentChanged = true;
        _image = image;
    }

    public void TranscodeImage(int imageFormat, IProgressContext? progress = null)
    {
        if (IsImageLocked)
            throw new InvalidOperationException("Image cannot be transcoded to another format.");

        throw new InvalidOperationException("Transcoding image is not supported for static images.");
    }

    public IList<Rgba32> GetPalette(IProgressContext? progress = null)
    {
        if (!IsIndexed)
            throw new InvalidOperationException("Image is not indexed.");

        throw new InvalidOperationException("Getting palette is not supported for static images.");
    }

    public void SetPalette(IList<Rgba32> palette, IProgressContext? progress = null)
    {
        if (!IsIndexed)
            throw new InvalidOperationException("Image is not indexed.");

        throw new InvalidOperationException("Setting palette is not supported for static images.");
    }

    public void TranscodePalette(int paletteFormat, IProgressContext? progress = null)
    {
        if (!IsIndexed)
            throw new InvalidOperationException("Image is not indexed.");

        throw new InvalidOperationException("Transcoding palette is not supported for static images.");
    }

    public void SetColorInPalette(int paletteIndex, Rgba32 color)
    {
        if (!IsIndexed)
            throw new InvalidOperationException("Image is not indexed.");

        throw new InvalidOperationException("Setting color in palette is not supported for static images.");
    }

    public void SetIndexInImage(Point point, int paletteIndex)
    {
        if (!IsIndexed)
            throw new InvalidOperationException("Image is not indexed.");

        throw new InvalidOperationException("Setting index in image is not supported for static images.");
    }
}