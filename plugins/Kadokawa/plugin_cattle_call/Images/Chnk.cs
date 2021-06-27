using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas;
using Kanvas.Encoding;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;
using Kontract.Models.Image;
using plugin_cattle_call.Compression;

namespace plugin_cattle_call.Images
{
    class Chnk
    {
        public IList<IKanvasImage> Load(Stream input)
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
            var texInfo = infBr.ReadType<ChnkInfo>();

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
            var result = new List<IKanvasImage>();
            if (imageFormat != -1)
            {
                var definition = ChnkSupport.GetEncodingDefinition();
                foreach (var dataChunk in dataChunks)
                {
                    var imageInfo = new ImageInfo(dataChunk.data, imageFormat, new Size(texInfo.width, texInfo.height));

                    if (imageFormat < 7)
                    {
                        imageInfo.PaletteData = paletteChunk.data;
                        imageInfo.PaletteFormat = 0;
                    }

                    imageInfo.PadSize.Width.ToPowerOfTwo();

                    result.Add(new KanvasImage(definition, imageInfo));
                }
            }
            else
            {
                // Expand TX4I data to RGBA8888
                foreach (var dataChunk in dataChunks)
                    result.Add(new BitmapKanvasImage(ExpandTX4I(dataChunk.data, tx4iChunk.data, paletteChunk.data)
                        .ToBitmap(new Size(texInfo.width, texInfo.height), new Size(paddedWidth, texInfo.height),
                            new BcSwizzle(new SwizzlePreparationContext(new Rgba(8, 8, 8, 8), new Size(paddedWidth, texInfo.height))), ImageAnchor.TopLeft)));
            }

            return result;
        }

        public void Save(Stream output, IList<IKanvasImage> images)
        {

        }

        private IList<ChnkSection> ReadSections(BinaryReaderX br)
        {
            // Read raw chunks
            var chunks = new List<ChnkSection>();
            while (br.BaseStream.Position < br.BaseStream.Length)
                chunks.Add(br.ReadType<ChnkSection>());

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

        private IList<Color> ExpandTX4I(byte[] data, byte[] tx4iData, byte[] paletteData)
        {
            var palEnc = new Rgba(5, 5, 5, "BGR");
            Color DecodeColor(byte[] cData) => palEnc.Load(cData, new EncodingLoadContext(new Size(1, 1), 1)).First();

            var result = new List<Color>();
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
    }
}
