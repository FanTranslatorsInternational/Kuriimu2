using System.Buffers.Binary;
using Kanvas;
using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Enums;
using Kanvas.Encoding;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using plugin_cattle_call.Compression;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace plugin_cattle_call.Images
{
    class Chnk
    {
        public List<IImageFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read chunks
            var chunks = ReadSections(br);

            // Flatten chunks
            var infChunk = chunks.FirstOrDefault(x => x.sectionMagic == "TXIF");
            var dataChunks = chunks.Where(x => x.sectionMagic == "TXIM").ToArray();
            var tx4iChunk = chunks.FirstOrDefault(x => x.sectionMagic == "TX4I");
            var paletteChunk = chunks.FirstOrDefault(x => x.sectionMagic == "TXPL");

            // Read information chunk
            using var infBr = new BinaryReaderX(new MemoryStream(infChunk.data));
            var texInfo = ReadChunkInfo(infBr);

            // Detect index depth by data length and palette size
            var paddedWidth = ChnkSupport.ToPowerOfTwo(texInfo.width);
            var bitDepth = texInfo.dataSize * 8 / paddedWidth / texInfo.height;

            // Detect image format
            var imageFormat = -1;
            if (tx4iChunk == null)
            {
                switch (bitDepth)
                {
                    case 2:
                        imageFormat = 2;
                        break;

                    case 4:
                        imageFormat = 3;
                        break;

                    case 8:
                        switch (paletteChunk.data.Length / 2)
                        {
                            case 8: imageFormat = 6; break;
                            case 32: imageFormat = 1; break;
                            case 256: imageFormat = 4; break;
                        }
                        break;

                    case 16:
                        imageFormat = 7;
                        break;
                }
            }

            // Create image info
            var result = new List<IImageFile>();
            if (imageFormat != -1)
            {
                var definition = ChnkSupport.GetEncodingDefinition();
                foreach (var dataChunk in dataChunks)
                {
                    var imageInfo = new ImageFileInfo
                    {
                        BitDepth = !ChnkSupport.ColorFormats.TryGetValue(imageFormat, out var encoding)
                            ? ChnkSupport.IndexFormats[imageFormat].BitDepth
                            : encoding.BitDepth,
                        ImageData = dataChunk.data,
                        ImageFormat = imageFormat,
                        ImageSize = new Size(texInfo.width, texInfo.height),
                        PadSize = builder => builder.Width.ToPowerOfTwo()
                    };

                    if (imageFormat < 7)
                    {
                        imageInfo.PaletteData = paletteChunk.data;
                        imageInfo.PaletteFormat = 0;
                    }

                    result.Add(new ImageFile(imageInfo, definition));
                }
            }
            else
            {
                // Expand TX4I data to RGBA8888
                foreach (var dataChunk in dataChunks)
                {
                    var expandedColors = ExpandTX4I(dataChunk.data, tx4iChunk.data, paletteChunk.data);

                    var size = new Size(texInfo.width, texInfo.height);
                    var paddedSize = new Size(paddedWidth, texInfo.height);
                    var swizzle = new BcSwizzle(new SwizzleOptions
                    {
                        Size = new Size(paddedWidth, texInfo.height),
                        EncodingInfo = new Rgba(8, 8, 8, 8)
                    });
                    var expandedImage = expandedColors.ToImage(size, paddedSize, swizzle, ImageAnchor.TopLeft);

                    result.Add(new StaticImageFile(expandedImage));
                }
            }

            return result;
        }

        private IList<ChnkSection> ReadSections(BinaryReaderX br)
        {
            // Read raw chunks
            var chunks = new List<ChnkSection>();
            while (br.BaseStream.Position < br.BaseStream.Length)
                chunks.Add(ReadSection(br));

            // Decompress chunk data
            foreach (var chunk in chunks)
            {
                if (chunk.decompressedSize == 0)
                    continue;

                var ms = new MemoryStream();
                NintendoCompressor.Decompress(new MemoryStream(chunk.data), ms);

                ms.Position = 0;
                chunk.data = ms.ToArray();
            }

            return chunks;
        }

        private IList<Rgba32> ExpandTX4I(byte[] data, byte[] tx4iData, byte[] paletteData)
        {
            var palEnc = new Rgba(5, 5, 5, "BGR");
            Rgba32 DecodeColor(byte[] cData) => palEnc.Load(cData, new EncodingOptions
            {
                Size = new Size(1, 1),
                TaskCount = 1
            }).First();

            var result = new List<Rgba32>();
            var clrBuffer = new byte[2];

            for (var i = 0; i < data.Length; i += 4)
            {
                var texBlock = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(i, 4));
                var tx4iBlock = BinaryPrimitives.ReadUInt16LittleEndian(tx4iData.AsSpan(i >> 1, 2));

                var palOffset = (tx4iBlock & 0x3FFF) * 4;
                var mode = tx4iBlock >> 14;

                var c0Value = (uint)BinaryPrimitives.ReadUInt16LittleEndian(paletteData.AsSpan(palOffset, 2));
                var c1Value = (uint)BinaryPrimitives.ReadUInt16LittleEndian(paletteData.AsSpan(palOffset + 2, 2));
                var c0 = DecodeColor(paletteData.AsSpan(palOffset, 2).ToArray());
                var c1 = DecodeColor(paletteData.AsSpan(palOffset + 2, 2).ToArray());

                for (var j = 0; j < 16; j++)
                {
                    var index = (texBlock >> (j * 2)) & 0x3;

                    switch (index)
                    {
                        case 0:
                            result.Add(c0);
                            break;

                        case 1:
                            result.Add(c1);
                            break;

                        case 2:
                            switch (mode)
                            {
                                case 0:
                                case 2:
                                    result.Add(DecodeColor(paletteData.AsSpan(palOffset + 4, 2).ToArray()));
                                    break;

                                case 1:
                                    result.Add(c0.InterpolateHalf(c1));
                                    break;

                                case 3:
                                    result.Add(c0.InterpolateEighth(c1, 5));
                                    break;
                            }
                            break;

                        case 3:
                            switch (mode)
                            {
                                case 0:
                                case 1:
                                    result.Add(Color.Transparent);
                                    break;

                                case 2:
                                    result.Add(DecodeColor(paletteData.AsSpan(palOffset + 6, 2).ToArray()));
                                    break;

                                case 3:
                                    result.Add(c0.InterpolateEighth(c1, 3));
                                    break;
                            }
                            break;
                    }
                }
            }

            return result;
        }

        private ChnkInfo ReadChunkInfo(BinaryReaderX reader)
        {
            return new ChnkInfo
            {
                unk1 = reader.ReadInt16(),
                unk2 = reader.ReadInt16(),
                dataSize = reader.ReadInt32(),
                tx4iSize = reader.ReadInt32(),
                paletteDataSize = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                imgCount = reader.ReadInt16(),
                unk3 = reader.ReadInt16()
            };
        }

        private ChnkSection ReadSection(BinaryReaderX reader)
        {
            var section = new ChnkSection
            {
                magic = reader.ReadString(4),
                decompressedSize = reader.ReadUInt32(),
                sectionMagic = reader.ReadString(4),
                sectionSize = reader.ReadInt32()
            };

            section.data = reader.ReadBytes(section.sectionSize);

            return section;
        }
    }
}
