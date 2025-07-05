using BCnEncoder.Shared;
using BCnEncoder.Shared.ImageFiles;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_khronos_group.Images
{
    class Ktx
    {
        public ImageFileInfo Load(Stream input)
        {
            // Load Ktx file
            var ktxFile = KtxFile.Load(input);

            // Prepare main image data
            var imageData = ktxFile.MipMaps[0].Faces[0].Data;
            var imageFormat = (int)ktxFile.header.GlInternalFormat;
            var size = new Size((int)ktxFile.header.PixelWidth, (int)ktxFile.header.PixelHeight);

            // Prepare mip maps
            return new ImageFileInfo
            {
                BitDepth = KtxSupport.Formats[imageFormat].BitDepth,
                ImageData = imageData,
                ImageFormat = imageFormat,
                ImageSize = size,
                MipMapData = ktxFile.MipMaps.Skip(1).Select(x => x.Faces[0].Data).ToArray()
            };
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            // Create Ktx file
            var ktxHeader = CreateKtxHeader(imageInfo.ImageFormat, imageInfo.ImageSize);
            var ktxFile = new KtxFile(ktxHeader);

            // Add main image to Ktx
            ktxFile.MipMaps.Add(CreateMipMap(imageInfo.ImageData, imageInfo.ImageSize));

            // Add mips to Ktx
            for (var i = 1; i <= (imageInfo.MipMapData?.Count ?? 0); i++)
            {
                ktxFile.MipMaps.Add(CreateMipMap(imageInfo.MipMapData[i - 1], new Size(imageInfo.ImageSize.Width >> i, imageInfo.ImageSize.Height >> i)));
            }

            ktxFile.Write(output);
        }

        private KtxMipmap CreateMipMap(byte[] data, Size mipSize)
        {
            var mip = new KtxMipmap((uint)data.Length, (uint)mipSize.Width, (uint)mipSize.Height, 1);
            mip.Faces[0] = new KtxMipFace(data, (uint)mipSize.Width, (uint)mipSize.Height);

            return mip;
        }

        private KtxHeader CreateKtxHeader(int format, Size size)
        {
            var glFormat = (GlInternalFormat)format;
            switch (glFormat)
            {
                case GlInternalFormat.GlCompressedRgb8PunchthroughAlpha1Etc2:
                case GlInternalFormat.GlCompressedRgba8Etc2Eac:
                    return KtxHeader.InitializeCompressed(size.Width, size.Height, glFormat, GlFormat.GlRgba);

                case GlInternalFormat.GlCompressedRgb8Etc2:
                    return KtxHeader.InitializeCompressed(size.Width, size.Height, glFormat, GlFormat.GlRgb);

                default:
                    throw new InvalidOperationException($"{glFormat} is not supported for saving.");
            }
        }
    }
}
