using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_grezzo.Images
{
	/* Original understanding by xdaniel and his tool Tharsis
	 * https://github.com/xdanieldzd/Tharsis */
	 
    class Ctxb
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(CtxbHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(CtxbEntry));

        public IList<ImageInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            var header = br.ReadType<CtxbHeader>();

            // Read chunks
            input.Position = header.chunkOffset;
            var chunks = br.ReadMultiple<CtxbChunk>((int)header.chunkCount);

            // Read images
            var infos = new List<ImageInfo>();
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
                    var mipMaps = new byte[texture.mipLvl-1][];
                    for (var j = 1; j < texture.mipLvl; j++)
                        mipMaps[j-1] = br.ReadBytes((texture.width >> j) * (texture.width >> j) * bitDepth / 8);

                    var imageInfo = new CtxbImageInfo(imageData, format, new Size(texture.width, texture.height), i, texture)
                    {
                        MipMapData = mipMaps,
                        Name = texture.name,
                    };

                    imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

                    infos.Add(imageInfo);
                }
            }

            return infos;
        }

        public void Save(Stream output, IList<ImageInfo> images)
        {
            using var bw=new BinaryWriterX(output);

            // Calculate offsets
            var chunkOffset = HeaderSize;
            var texDataOffset = chunkOffset + images.Count * EntrySize +
                                images.Cast<CtxbImageInfo>().GroupBy(x => x.ChunkIndex).Count() * 0xC;

            // Write image data
            var texDataPosition = texDataOffset;

            var entries = new List<(int, CtxbEntry)>();
            foreach (var imageInfo in images.Cast<CtxbImageInfo>())
            {
                var dataOffset = texDataPosition - texDataOffset;
                var dataLength = imageInfo.ImageData.Length;

                output.Position = texDataOffset;
                output.Write(imageInfo.ImageData);

                // Write mipmaps
                for (var i = 0; i < imageInfo.MipMapCount; i++)
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
                var chunkEntry=new CtxbChunk
                {
                    texCount = chunk.Count(),
                    textures = chunk.Select(x=>x.Item2).ToArray(),
                    chunkSize = 0xC+chunk.Count()*EntrySize
                };

                bw.WriteType(chunkEntry);
            }

            // Write header
            var header=new CtxbHeader
            {
                fileSize = (int)output.Length,
                chunkOffset = chunkOffset,
                chunkCount = chunks.Length,
                texDataOffset = texDataOffset
            };

            output.Position = 0;
            bw.WriteType(header);
        }
    }
}
