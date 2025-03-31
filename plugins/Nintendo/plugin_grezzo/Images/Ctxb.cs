using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_grezzo.Images
{
    /* Original understanding by xdaniel and his tool Tharsis
	 * https://github.com/xdanieldzd/Tharsis */

    class Ctxb
    {
        private const int HeaderSize_ = 0x18;
        private const int EntrySize_ = 0x24;

        public List<ImageFileInfo> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input);

            // Read header
            var header = typeReader.Read<CtxbHeader>(br);

            // Read chunks
            input.Position = header.chunkOffset;
            var chunks = typeReader.ReadMany<CtxbChunk>(br, (int)header.chunkCount);

            // Read images
            var infos = new List<ImageFileInfo>();
            for (var i = 0; i < chunks.Count; i++)
            {
                foreach (var texture in chunks[i].textures)
                {
                    input.Position = header.texDataOffset + texture.dataOffset;

                    // imageFormat is ignored if ETC1(a4)
                    var format = texture.isETC1 ? texture.imageFormat : (texture.dataType << 16) | texture.imageFormat;
                    var bitDepth = CtxbSupport.CtxbFormats[(uint)format].BitDepth;

                    var dataLength = texture.width * texture.height * bitDepth / 8;
                    var imageData = br.ReadBytes(dataLength);

                    // Read mip maps
                    var mipMaps = new byte[texture.mipLvl - 1][];
                    for (var j = 1; j < texture.mipLvl; j++)
                        mipMaps[j - 1] = br.ReadBytes((texture.width >> j) * (texture.width >> j) * bitDepth / 8);

                    var imageInfo = new CtxbImageFileInfo(i, texture)
                    {
                        Name = texture.name,
                        ImageData = imageData,
                        MipMapData = mipMaps,
                        BitDepth = bitDepth,
                        ImageFormat = format,
                        ImageSize = new Size(texture.width, texture.height),
                        RemapPixels = context => new CtrSwizzle(context)
                    };

                    infos.Add(imageInfo);
                }
            }

            return infos;
        }

        public void Save(Stream output, List<ImageFileInfo> images)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var chunkOffset = HeaderSize_;
            var texDataOffset = chunkOffset + images.Count * EntrySize_ +
                                images.Cast<CtxbImageFileInfo>().GroupBy(x => x.ChunkIndex).Count() * 0xC;

            // Write image data
            var texDataPosition = texDataOffset;

            var entries = new List<(int, CtxbEntry)>();
            foreach (var imageInfo in images.Cast<CtxbImageFileInfo>())
            {
                var dataOffset = texDataPosition - texDataOffset;
                var dataLength = imageInfo.ImageData.Length;

                output.Position = texDataOffset;
                output.Write(imageInfo.ImageData);

                // Write mipmaps
                for (var i = 0; i < (imageInfo.MipMapData?.Count ?? 0); i++)
                {
                    output.Write(imageInfo.MipMapData[i]);
                    dataLength += imageInfo.MipMapData[i].Length;
                    texDataPosition += imageInfo.MipMapData[i].Length;
                }

                texDataPosition += imageInfo.ImageData.Length;

                entries.Add((imageInfo.ChunkIndex, new CtxbEntry
                {
                    dataOffset = dataOffset,
                    dataLength = dataLength,
                    width = (short)imageInfo.ImageSize.Width,
                    height = (short)imageInfo.ImageSize.Height,
                    dataType = (ushort)(imageInfo.ImageFormat >> 16),
                    imageFormat = (ushort)imageInfo.ImageFormat,
                    mipLvl = imageInfo.Entry.mipLvl,
                    isETC1 = ((imageInfo.ImageFormat & 0xFFFF) == 0x675A || (imageInfo.ImageFormat & 0xFFFF) == 0x675B) ? true : false,
                    isCubemap = imageInfo.Entry.isCubemap,
                    name = imageInfo.Entry.name.PadRight(0x10).Substring(0, 0x10)
                }));
            }

            // Write chunk entries
            output.Position = chunkOffset;

            var chunks = entries.GroupBy(x => x.Item1).ToArray();
            foreach (var chunk in chunks)
            {
                var chunkEntry = new CtxbChunk
                {
                    texCount = chunk.Count(),
                    textures = chunk.Select(x => x.Item2).ToArray(),
                    chunkSize = 0xC + chunk.Count() * EntrySize_
                };

                typeWriter.Write(chunkEntry, bw);
            }

            // Write header
            var header = new CtxbHeader
            {
                fileSize = (int)output.Length,
                chunkOffset = chunkOffset,
                chunkCount = chunks.Length,
                texDataOffset = texDataOffset
            };

            output.Position = 0;
            typeWriter.Write(header, bw);
        }
    }
}
