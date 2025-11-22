using Kanvas.Configuration;
using Kanvas.Contract;
using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Contract.Progress;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Kanvas.Contract.Configuration;

namespace Konnect.Plugin.File.Image;

public class ImageFile : IImageFile
{
    private Image<Rgba32>? _decodedImage;
    private IList<Rgba32>? _decodedPalette;

    private Image<Rgba32>? _bestImage;

    #region Properties

    /// <inheritdoc />
    public IEncodingDefinition EncodingDefinition { get; }

    /// <inheritdoc />
    public ImageFileInfo ImageInfo { get; }

    /// <inheritdoc />
    public bool IsIndexed => IsIndexEncoding(ImageInfo.ImageFormat);

    /// <inheritdoc />
    public bool IsImageLocked { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="ImageInfo"/>.
    /// </summary>
    /// <param name="imageInfo">The image info to represent.</param>
    /// <param name="encodingDefinition">The definition of encodings to use on the data.</param>
    public ImageFile(ImageFileInfo imageInfo, IEncodingDefinition encodingDefinition)
    {
        ImageInfo = imageInfo;
        EncodingDefinition = encodingDefinition;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ImageInfo"/>.
    /// </summary>
    /// <param name="imageInfo">The image info to represent.</param>
    /// <param name="encodingDefinition">The definition of encodings to use on the data.</param>
    /// <param name="lockImage">Locks the image to its initial dimension and encodings. This will throw an exception in the methods that may try such changes.</param>
    public ImageFile(ImageFileInfo imageInfo, bool lockImage, IEncodingDefinition encodingDefinition)
        : this(imageInfo, encodingDefinition)
    {
        IsImageLocked = lockImage;
    }

    #endregion

    #region Static

    /// <summary>
    /// Creates a new image file.
    /// </summary>
    /// <param name="imageSize">The size of the new image.</param>
    /// <param name="encodingDefinition">The encodings to transcode the images into.</param>
    /// <returns>The new image file.</returns>
    public static ImageFile Create(Size imageSize, IEncodingDefinition encodingDefinition)
    {
        var bitDepth = 0;
        var format = 0;

        int paletteBitDepth = -1;
        int paletteFormat = -1;
        byte[]? paletteData = null;

        if (encodingDefinition.ColorEncodings.Count > 0)
        {
            (format, IColorEncoding encoding) = encodingDefinition.ColorEncodings.First();
            bitDepth = encoding.BitDepth;
        }
        else if (encodingDefinition.IndexEncodings.Count > 0)
        {
            (format, IndexEncodingDefinition indexDefinition) = encodingDefinition.IndexEncodings.First();
            bitDepth = indexDefinition.IndexEncoding.BitDepth;

            paletteFormat = indexDefinition.PaletteEncodingIndices[0];
            paletteBitDepth = encodingDefinition.GetPaletteEncoding(paletteFormat)?.BitDepth ?? -1;
            paletteData = new byte[indexDefinition.IndexEncoding.MaxColors * paletteBitDepth];
        }

        var imageInfo = new ImageFileInfo
        {
            Name = string.Empty,
            BitDepth = bitDepth,
            ImageSize = imageSize,
            ImageData = new byte[imageSize.Width * imageSize.Height * bitDepth / 8],
            ImageFormat = format,
            PaletteBitDepth = paletteBitDepth,
            PaletteData = paletteData,
            PaletteFormat = paletteFormat,
            ContentChanged = false
        };

        return new(imageInfo, encodingDefinition);
    }

    /// <summary>
    /// Decodes an image from a <see cref="ImageFileInfo"/>.
    /// </summary>
    /// <param name="imageInfo">The image info containing all necessary image data and settings.</param>
    /// <param name="encodingDefinition">The encodings available for the image data.</param>
    /// <returns>The decoded image.</returns>
    public static Image<Rgba32> Decode(ImageFileInfo imageInfo, IEncodingDefinition encodingDefinition)
    {
        return new ImageFile(imageInfo, encodingDefinition).GetImage();
    }

    /// <summary>
    /// Encodes an image to a <see cref="ImageFileInfo"/>.
    /// </summary>
    /// <param name="image">The image to encode into <paramref name="imageInfo"/>.</param>
    /// <param name="imageInfo">The image info that will receive all the encoded image data.</param>
    /// <param name="encodingDefinition">The encodings available for the image data.</param>
    public static void Encode(Image<Rgba32> image, ImageFileInfo imageInfo, IEncodingDefinition encodingDefinition)
    {
        new ImageFile(imageInfo, encodingDefinition).SetImage(image);
    }

    #endregion

    #region Interface

    public IImageFile Clone()
    {
        var clonedImageData = new byte[ImageInfo.ImageData.Length];
        Array.Copy(ImageInfo.ImageData, clonedImageData, clonedImageData.Length);

        byte[]? clonedPaletteData = null;
        if (ImageInfo.PaletteData is not null)
        {
            clonedPaletteData = new byte[ImageInfo.PaletteData.Length];
            Array.Copy(ImageInfo.PaletteData, clonedPaletteData, clonedPaletteData.Length);
        }

        IList<byte[]>? clonedMipMapData = null;
        if (ImageInfo.MipMapData is not null)
        {
            clonedMipMapData = [];
            foreach (byte[] mipMapData in ImageInfo.MipMapData)
            {
                var clonedMipMap = new byte[mipMapData.Length];
                Array.Copy(mipMapData, clonedMipMap, clonedMipMap.Length);

                clonedMipMapData.Add(clonedMipMap);
            }
        }

        var clonedInfo = new ImageFileInfo
        {
            Name = ImageInfo.Name,
            BitDepth = ImageInfo.BitDepth,
            PaletteBitDepth = ImageInfo.PaletteBitDepth,
            ImageSize = ImageInfo.ImageSize,
            ImageData = clonedImageData,
            ImageFormat = ImageInfo.ImageFormat,
            PaletteData = clonedPaletteData,
            PaletteFormat = ImageInfo.PaletteFormat,
            MipMapData = clonedMipMapData,
            IsAnchoredAt = ImageInfo.IsAnchoredAt,
            PadSize = ImageInfo.PadSize,
            RemapPixels = ImageInfo.RemapPixels,
            ContentChanged = true
        };

        return new ImageFile(clonedInfo, EncodingDefinition);
    }

    #region Image methods

    /// <inheritdoc />
    public Image<Rgba32> GetImage(IProgressContext? progress = null)
    {
        if (_decodedImage != null)
            return _decodedImage;

        _decodedImage = GetDecodedImage();
        _bestImage ??= _decodedImage;

        return _decodedImage;
    }

    /// <inheritdoc />
    public void SetImage(Image<Rgba32> image, IProgressContext? progress = null)
    {
        // Check for locking
        if (IsImageLocked && (ImageInfo.ImageSize.Width != image.Width || ImageInfo.ImageSize.Height != image.Height))
            throw new InvalidOperationException("Only images with the same dimensions can be set.");

        _bestImage = image;

        _decodedImage = null;
        _decodedPalette = null;

        (IList<byte[]> imageData, byte[]? paletteData) = EncodeImage(image, ImageInfo.ImageFormat, ImageInfo.PaletteFormat);

        ImageInfo.ImageData = imageData[0];
        ImageInfo.MipMapData = imageData.Skip(1).ToArray();

        ImageInfo.PaletteData = paletteData;

        ImageInfo.ImageSize = image.Size;

        ImageInfo.ContentChanged = true;
    }

    /// <inheritdoc />
    public void TranscodeImage(int imageFormat, IProgressContext? progress = null)
    {
        if (IsImageLocked)
            throw new InvalidOperationException("Image can not be transcoded to another format.");

        IndexEncodingDefinition? indexEncoding = EncodingDefinition.GetIndexEncoding(imageFormat);

        int paletteFormat = ImageInfo.PaletteFormat;
        if (indexEncoding != null)
        {
            if (indexEncoding.PaletteEncodingIndices.Count <= 0)
                throw new InvalidOperationException($"No palette encodings are associated with encoding 0x{imageFormat:X2}.");

            if (!indexEncoding.PaletteEncodingIndices.Contains(paletteFormat))
                paletteFormat = indexEncoding.PaletteEncodingIndices.First();
        }

        TranscodeImage(imageFormat, paletteFormat);
    }

    /// <inheritdoc />
    public void SetIndexInImage(Point point, int paletteIndex)
    {
        if (!IsIndexed)
            throw new InvalidOperationException("Palette index can only be set on an index-encoded image.");

        Image<Rgba32> image = GetImage();
        if (!IsPointInRegion(point, image.Size))
            throw new InvalidOperationException($"Point {point} is outside the image.");

        IList<Rgba32> palette = GetPalette();
        if (paletteIndex >= palette.Count)
            throw new InvalidOperationException($"Palette index {paletteIndex} is out of range.");

        image[point.X, point.Y] = palette[paletteIndex];

        (IList<byte[]> imageData, byte[]? paletteData) = EncodeImage(image, ImageInfo.ImageFormat, ImageInfo.PaletteFormat);

        ImageInfo.ImageData = imageData[0];
        ImageInfo.MipMapData = imageData.Skip(1).ToArray();

        ImageInfo.PaletteData = paletteData;

        ImageInfo.ContentChanged = true;
    }

    #endregion

    #region Palette methods

    /// <inheritdoc />
    public IList<Rgba32> GetPalette(IProgressContext? progress = null)
    {
        if (!IsIndexed)
            throw new InvalidOperationException("Palette can only be retrieved from an index-encoded image.");

        if (ImageInfo.PaletteData == null)
            throw new InvalidOperationException("No palette data is set for this image.");

        if (_decodedPalette != null)
            return _decodedPalette;

        return _decodedPalette = GetDecodedPalette(ImageInfo.PaletteData, ImageInfo.PaletteFormat);
    }

    /// <inheritdoc />
    public void SetPalette(IList<Rgba32> palette, IProgressContext? progress = null)
    {
        if (!IsIndexed)
            throw new InvalidOperationException("Palette can only be set on an index-encoded image.");

        if (IsImageLocked)
        {
            if (ImageInfo.PaletteData == null)
            {
                if (palette.Count <= 0)
                    return;

                throw new InvalidOperationException("No palette data is set for this image and image is locked.");
            }

            IList<Rgba32> decodedPalette = GetDecodedPalette(ImageInfo.PaletteData, ImageInfo.PaletteFormat);
            if (palette.Count != decodedPalette.Count)
                throw new InvalidOperationException($"Only palettes with the same amount of colors can be set. (Expected color count: {decodedPalette.Count})");
        }

        _decodedImage = null;
        _bestImage = null;
        _decodedPalette = palette;

        ImageInfo.PaletteData = GetEncodedPalette(palette, ImageInfo.PaletteFormat);

        ImageInfo.ContentChanged = true;
    }

    /// <inheritdoc />
    public void TranscodePalette(int paletteFormat, IProgressContext? progress = null)
    {
        if (!IsIndexed)
            throw new InvalidOperationException("Palette can only be transcoded on an index-encoded image.");

        if (IsImageLocked)
            throw new InvalidOperationException("Palette can not be transcoded to another format.");

        TranscodeImage(ImageInfo.ImageFormat, paletteFormat);
    }

    /// <inheritdoc />
    public void SetColorInPalette(int paletteIndex, Rgba32 color)
    {
        if (!IsIndexed)
            throw new InvalidOperationException("Palette color can only be set on an index-encoded image.");

        IList<Rgba32> palette = GetPalette();
        if (paletteIndex >= palette.Count)
            throw new InvalidOperationException($"Palette index {paletteIndex} is out of range.");

        palette[paletteIndex] = color;

        SetPalette(palette);
    }

    #endregion

    #endregion

    #region Decode

    #region Decode image

    protected virtual Image<Rgba32> GetDecodedImage()
    {
        return IsIndexed
            ? GetDecodedIndexImage()
            : GetDecodedColorImage();
    }

    private Image<Rgba32> GetDecodedColorImage()
    {
        IImageTranscoder transcoder = CreateColorImageConfiguration(ImageInfo.ImageFormat).Build();
        return transcoder.Decode(ImageInfo.ImageData, ImageInfo.ImageSize);
    }

    private Image<Rgba32> GetDecodedIndexImage()
    {
        if (ImageInfo.PaletteData == null || ImageInfo.PaletteFormat < 0)
            throw new InvalidOperationException("Palette is not configured for an index-encoded image.");

        IImageTranscoder transcoder = CreateIndexImageConfiguration(ImageInfo.ImageFormat, ImageInfo.PaletteFormat).Build();
        return transcoder.Decode(ImageInfo.ImageData, ImageInfo.PaletteData, ImageInfo.ImageSize);
    }

    #endregion

    #region Decode palette

    private IList<Rgba32> GetDecodedPalette(byte[] paletteData, int paletteFormat)
    {
        IColorEncoding paletteEncoding = GetPaletteEncoding(paletteFormat);

        var options = new EncodingOptions
        {
            Size = new Size(1, paletteData.Length * 8 / paletteEncoding.BitsPerValue),
            TaskCount = Environment.ProcessorCount
        };

        return paletteEncoding.Load(paletteData, options).ToArray();
    }

    #endregion

    #endregion

    #region Encode

    private (IList<byte[]> imageData, byte[]? paletteData) EncodeImage(Image<Rgba32> image, int imageFormat, int paletteFormat)
    {
        int mipCount = ImageInfo.MipMapData?.Count ?? 0;
        var images = new byte[mipCount + 1][];

        // Encode image
        (byte[] imageData, byte[]? paletteData) = GetEncodedImage(image, imageFormat, paletteFormat);

        images[0] = imageData;

        // Decode palette, if present and needed for mip maps
        IList<Rgba32>? decodedPalette = null;
        if (paletteData != null && mipCount > 0)
            decodedPalette = GetDecodedPalette(paletteData, paletteFormat);

        // Encode mip maps
        (int width, int height) = (image.Width >> 1, image.Height >> 1);
        for (var i = 0; i < mipCount; i++)
        {
            Image<Rgba32> mipMap = ResizeImage(image, width, height);
            byte[] mipMapData = GetEncodedMipMap(mipMap, imageFormat, paletteFormat, decodedPalette);

            images[i + 1] = mipMapData;

            width >>= 1;
            height >>= 1;
        }

        return (images, paletteData);
    }

    private Image<Rgba32> ResizeImage(Image<Rgba32> image, int width, int height)
    {
        Image<Rgba32> cloned = image.Clone();
        cloned.Mutate(context => context.Resize(width, height));

        return cloned;
    }

    #region Encode image

    protected virtual (byte[], byte[]?) GetEncodedImage(Image<Rgba32> image, int imageFormat, int paletteFormat)
    {
        return IsIndexEncoding(imageFormat)
            ? GetEncodedIndexImage(image, imageFormat, paletteFormat)
            : GetEncodedColorImage(image, imageFormat);
    }

    private (byte[], byte[]?) GetEncodedColorImage(Image<Rgba32> image, int imageFormat)
    {
        IImageTranscoder transcoder = CreateColorImageConfiguration(imageFormat).Build();
        return transcoder.Encode(image);
    }

    private (byte[], byte[]?) GetEncodedIndexImage(Image<Rgba32> image, int imageFormat, int paletteFormat)
    {
        IImageTranscoder transcoder = CreateIndexImageConfiguration(imageFormat, paletteFormat).Build();
        return transcoder.Encode(image);
    }

    #endregion

    #region Encode mip map

    protected virtual byte[] GetEncodedMipMap(Image<Rgba32> image, int imageFormat, int paletteFormat, IList<Rgba32>? palette)
    {
        return IsIndexEncoding(imageFormat)
            ? GetEncodedIndexMipmap(image, imageFormat, paletteFormat, palette)
            : GetEncodedColorMipmap(image, imageFormat);
    }

    private byte[] GetEncodedColorMipmap(Image<Rgba32> image, int imageFormat)
    {
        IImageTranscoder transcoder = CreateColorImageConfiguration(imageFormat).Build();
        return transcoder.Encode(image).imageData;
    }

    private byte[] GetEncodedIndexMipmap(Image<Rgba32> image, int imageFormat, int paletteFormat, IList<Rgba32>? palette)
    {
        if (palette == null)
            throw new InvalidOperationException("No palette given for mip map encoding.");

        IImageTranscoder transcoder = CreateIndexImageConfiguration(imageFormat, paletteFormat, palette).Build();
        return transcoder.Encode(image).imageData;
    }

    #endregion

    #region Encode palette

    private byte[] GetEncodedPalette(IList<Rgba32> palette, int paletteFormat)
    {
        IColorEncoding paletteEncoding = GetPaletteEncoding(paletteFormat);

        var options = new EncodingOptions
        {
            Size = new Size(1, palette.Count),
            TaskCount = Environment.ProcessorCount
        };

        return paletteEncoding.Save(palette, options);
    }

    #endregion

    #endregion

    #region Transcode

    private void TranscodeImage(int imageFormat, int paletteFormat)
    {
        // Decode image
        Image<Rgba32> decodedImage = _bestImage ?? GetDecodedImage();

        // Encode image
        (IList<byte[]> imageData, byte[]? paletteData) = EncodeImage(decodedImage, imageFormat, paletteFormat);

        IEncodingInfo encodingInfo = GetEncodingInfo(imageFormat)!;
        IEncodingInfo? paletteEncodingInfo = GetPaletteEncodingInfo(paletteFormat);

        ImageInfo.BitDepth = encodingInfo.BitDepth;
        ImageInfo.ImageData = imageData[0];
        ImageInfo.MipMapData = imageData.Skip(1).ToArray();
        ImageInfo.ImageFormat = imageFormat;

        ImageInfo.PaletteBitDepth = paletteEncodingInfo?.BitDepth ?? -1;
        ImageInfo.PaletteData = paletteData;
        ImageInfo.PaletteFormat = paletteEncodingInfo != null ? paletteFormat : -1;

        ImageInfo.ContentChanged = true;

        _decodedImage = null;
        _decodedPalette = null;
    }

    private IEncodingInfo? GetEncodingInfo(int imageFormat)
    {
        if (IsIndexEncoding(imageFormat))
            return EncodingDefinition.GetIndexEncoding(imageFormat)?.IndexEncoding;

        return EncodingDefinition.GetColorEncoding(imageFormat);
    }

    private IEncodingInfo? GetPaletteEncodingInfo(int paletteFormat)
    {
        return EncodingDefinition.GetPaletteEncoding(paletteFormat);
    }

    #endregion

    #region Configurations

    private ImageConfigurationBuilder CreateColorImageConfiguration(int imageFormat)
    {
        ImageConfigurationBuilder config = CreateImageConfiguration();

        IColorShader? colorShader = EncodingDefinition.GetColorShader(imageFormat);
        if (colorShader != null)
            config.ShadeColors.With(() => colorShader);

        IColorEncoding encoding = GetColorEncoding(imageFormat);
        config.Transcode.With(encoding);

        return config;
    }

    private ImageConfigurationBuilder CreateIndexImageConfiguration(int imageFormat, int paletteFormat, IList<Rgba32> palette)
    {
        ImageConfigurationBuilder config = CreateIndexImageConfiguration(imageFormat, paletteFormat);

        config.ConfigureQuantization(options => options.WithPalette(() => palette));

        return config;
    }

    private ImageConfigurationBuilder CreateIndexImageConfiguration(int imageFormat, int paletteFormat)
    {
        ImageConfigurationBuilder config = CreateImageConfiguration();

        if (ImageInfo.Quantize is null)
            config.ConfigureQuantization(options => options);

        IColorShader? paletteShader = EncodingDefinition.GetPaletteShader(paletteFormat);
        if (paletteShader != null)
            config.ShadeColors.With(() => paletteShader);

        IIndexEncoding encoding = GetIndexEncoding(imageFormat);
        IIndexedImageConfigurationBuilder indexConfig = config.Transcode.With(encoding);

        IColorEncoding paletteEncoding = GetPaletteEncoding(paletteFormat);
        indexConfig.TranscodePalette.With(paletteEncoding);

        return config;
    }

    private ImageConfigurationBuilder CreateImageConfiguration()
    {
        var config = new ImageConfigurationBuilder();

        config.IsAnchoredAt(ImageInfo.IsAnchoredAt);

        if (ImageInfo.PadSize != null)
            config.PadSize.To(ImageInfo.PadSize);

        if (ImageInfo.RemapPixels != null)
            config.RemapPixels.With(ImageInfo.RemapPixels);

        if (ImageInfo.Quantize != null)
            config.ConfigureQuantization(options => ImageInfo.Quantize(options));

        return config;
    }

    private IColorEncoding GetColorEncoding(int imageFormat)
    {
        IColorEncoding? encoding = EncodingDefinition.GetColorEncoding(imageFormat);
        if (encoding == null)
            throw new InvalidOperationException($"Unknown encoding 0x{imageFormat:X2}.");

        return encoding;
    }

    private IIndexEncoding GetIndexEncoding(int imageFormat)
    {
        IIndexEncoding? encoding = EncodingDefinition.GetIndexEncoding(imageFormat)?.IndexEncoding;
        if (encoding == null)
            throw new InvalidOperationException($"Unknown encoding 0x{imageFormat:X2}.");

        return encoding;
    }

    private IColorEncoding GetPaletteEncoding(int paletteFormat)
    {
        IColorEncoding? encoding = EncodingDefinition.GetPaletteEncoding(paletteFormat);
        if (encoding == null)
            throw new InvalidOperationException($"Unknown palette encoding 0x{paletteFormat:X2}.");

        return encoding;
    }

    #endregion

    private bool IsPointInRegion(Point point, Size region)
    {
        var rectangle = new Rectangle(Point.Empty, region);
        return rectangle.Contains(point);
    }

    private bool IsIndexEncoding(int imageFormat)
        => EncodingDefinition.ContainsIndexEncoding(imageFormat);
}