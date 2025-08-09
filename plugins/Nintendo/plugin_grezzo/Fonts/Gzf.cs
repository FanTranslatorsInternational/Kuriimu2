using Kanvas.Contract.Encoding;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Generation;
using Konnect.Plugin.File.Image;
using SixLabors.ImageSharp.Processing;
using Konnect.Contract.DataClasses.Plugin.File.Image;

namespace plugin_grezzo.Fonts
{
    class Gzf
    {
        private GzfHeader _header;

        public List<CharacterInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            _header = ReadHeader(br);

            input.Position = _header.imgInfoOffset;
            GzfImageInfo[] imageInfos = ReadImageInfos(br, _header.imgCount);

            GzfEntry[] entries = ReadEntries(br, _header.entryCount);

            EncodingDefinition encodingDefinition = GzfSupport.GetEncodingDefinition();

            var images = new List<Image<Rgba32>>();
            foreach (GzfImageInfo imageInfo in imageInfos)
            {
                IColorEncoding encoding = GzfSupport.Formats[_header.format];

                int bitDepth = encoding.BitDepth;
                int dataLength = imageInfo.width * imageInfo.height * bitDepth;

                input.Position = imageInfo.offset;
                byte[] imageData = br.ReadBytes(dataLength);

                var imageFileInfo = new ImageFileInfo
                {
                    BitDepth = encoding.BitDepth,
                    ImageData = imageData,
                    ImageFormat = _header.format,
                    ImageSize = new Size(imageInfo.width, imageInfo.height),
                    RemapPixels = context => new CtrSwizzle(context)
                };

                Image<Rgba32> rawGlyph = ImageFile.Decode(imageFileInfo, encodingDefinition);
                images.Add(rawGlyph);
            }

            var result = new List<CharacterInfo>();

            foreach (GzfEntry entry in entries)
            {
                int x = entry.column * _header.glyphWidth;
                int y = entry.row * _header.glyphHeight;

                Image<Rgba32> image = images[entry.imageIndex];
                GlyphDescriptionData glyphDescription = WhiteSpaceMeasurer.MeasureWhiteSpace(image, new Rectangle(x, y, _header.glyphWidth, _header.glyphHeight));

                if (glyphDescription.Size is { Width: > 0, Height: > 0 })
                {
                    result.Add(new CharacterInfo
                    {
                        CodePoint = (char)entry.codePoint,
                        GlyphPosition = new Point(glyphDescription.Position.X - x - entry.posX, glyphDescription.Position.Y - y),
                        BoundingBox = new Size(entry.charWidth, _header.glyphHeight - (glyphDescription.Position.Y - y)),
                        Glyph = image.Clone(context => context.Crop(new Rectangle(glyphDescription.Position, glyphDescription.Size)))
                    });
                }
                else if (entry.charWidth > 0)
                {
                    result.Add(new CharacterInfo
                    {
                        CodePoint = (char)entry.codePoint,
                        GlyphPosition = Point.Empty,
                        BoundingBox = new Size(entry.charWidth, _header.glyphHeight),
                        Glyph = image.Clone(context => context.Crop(new Rectangle(x + entry.posX, y, entry.charWidth, _header.glyphHeight)))
                    });
                }
                else
                {
                    result.Add(new CharacterInfo
                    {
                        CodePoint = (char)entry.codePoint,
                        GlyphPosition = new Point(entry.posX, 0),
                        BoundingBox = new Size(entry.charWidth, _header.glyphHeight),
                        Glyph = null
                    });
                }
            }

            return result;
        }

        public void Save(Stream output, List<CharacterInfo> characters)
        {
            using var bw = new BinaryWriterX(output);

            List<GzfEntry> entries = CreateEntries(characters, out List<Image<Rgba32>> images);

            int dataOffset = (_header.imgInfoOffset + images.Count * 8 + entries.Count * 0xC + 0x7F) & ~0x7F;
            List<GzfImageInfo> imageInfos = CreateImageInfos(images, dataOffset, out List<byte[]> imageData);

            _header.entryCount = entries.Count;
            _header.imgCount = images.Count;

            WriteHeader(_header, bw);

            output.Position = _header.imgInfoOffset;
            WriteImageInfos(imageInfos, bw);
            WriteEntries(entries, bw);

            output.Position = dataOffset;
            foreach (byte[] data in imageData)
                bw.Write(data);
        }

        private List<GzfEntry> CreateEntries(List<CharacterInfo> characters, out List<Image<Rgba32>> images)
        {
            _header.glyphWidth = (short)((characters.Max(x => x.Glyph?.Width ?? 0) + 7) & ~7);
            _header.glyphHeight = (short)(characters.Max(x => x.GlyphPosition.Y + x.BoundingBox.Height));

            var imageSize = new Size(0x100, 0x100);

            int glyphsPerRow = imageSize.Width / _header.glyphWidth;
            int rowsPerImage = imageSize.Height / _header.glyphHeight;
            int glyphsPerImage = rowsPerImage * glyphsPerRow;

            var result = new List<GzfEntry>();

            images = [];
            for (var i = 0; i < characters.Count; i++)
            {
                CharacterInfo character = characters[i];

                int imageIndex = i / glyphsPerImage;
                int row = i % glyphsPerImage / glyphsPerRow;
                int column = i % glyphsPerImage % glyphsPerRow;

                if (row is 0 && column is 0)
                    images.Add(new Image<Rgba32>(imageSize.Width, imageSize.Height));

                int posX = character.Glyph is null
                    ? character.GlyphPosition.X
                    : (_header.glyphWidth - character.Glyph.Width) / 2 - character.GlyphPosition.X;

                result.Add(new GzfEntry
                {
                    codePoint = character.CodePoint,
                    posX = (short)posX,
                    charWidth = (short)character.BoundingBox.Width,
                    imageIndex = (short)imageIndex,
                    row = (byte)row,
                    column = (byte)column
                });

                if (character.Glyph is null)
                    continue;

                int x = column * _header.glyphWidth + (_header.glyphWidth - character.Glyph.Width) / 2;
                int y = row * _header.glyphHeight + character.GlyphPosition.Y;

                images[^1].Mutate(context => context.DrawImage(character.Glyph, new Point(x, y), 1f));
            }

            if (images.Count <= 0)
                return result;

            int lastGlyphCount = characters.Count % glyphsPerImage;
            int lastGlyphRows = lastGlyphCount / glyphsPerRow + (lastGlyphCount % glyphsPerRow > 0 ? 1 : 0);
            int destHeight = lastGlyphRows * _header.glyphHeight;

            int width = images[^1].Width;
            int height = Kanvas.SizePadding.PowerOfTwo(destHeight);

            images[^1].Mutate(context => context.Crop(width, height));

            return result;
        }

        private List<GzfImageInfo> CreateImageInfos(List<Image<Rgba32>> images, int dataOffset, out List<byte[]> imageData)
        {
            imageData = [];

            EncodingDefinition encodingDefinition = GzfSupport.GetEncodingDefinition();

            var result = new List<GzfImageInfo>();

            foreach (Image<Rgba32> image in images)
            {
                IColorEncoding encoding = GzfSupport.Formats[_header.format];
                var imageInfo = new ImageFileInfo
                {
                    BitDepth = encoding.BitDepth,
                    ImageData = [],
                    ImageFormat = _header.format,
                    ImageSize = Size.Empty,
                    RemapPixels = context => new CtrSwizzle(context)
                };

                ImageFile.Encode(image, imageInfo, encodingDefinition);
                imageData.Add(imageInfo.ImageData);

                result.Add(new GzfImageInfo
                {
                    offset = dataOffset,
                    width = (short)image.Width,
                    height = (short)image.Height
                });

                dataOffset += imageInfo.ImageData.Length;
            }

            return result;
        }

        private GzfHeader ReadHeader(BinaryReaderX reader)
        {
            return new GzfHeader
            {
                magic = reader.ReadString(4),
                version = reader.ReadInt32(),
                imgInfoOffset = reader.ReadUInt16(),
                imgInfoSize = reader.ReadUInt16(),
                entrySize = reader.ReadInt32(),
                imgCount = reader.ReadInt32(),
                entryCount = reader.ReadInt32(),
                unk2 = reader.ReadUInt32(),
                unk3 = reader.ReadUInt32(),
                format = reader.ReadInt16(),
                unk4 = reader.ReadInt16(),
                unk5 = reader.ReadInt16(),
                glyphWidth = reader.ReadInt16(),
                glyphHeight = reader.ReadInt16()
            };
        }

        private GzfImageInfo[] ReadImageInfos(BinaryReaderX reader, int count)
        {
            var result = new GzfImageInfo[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadImageInfo(reader);

            return result;
        }

        private GzfImageInfo ReadImageInfo(BinaryReaderX reader)
        {
            return new GzfImageInfo
            {
                offset = reader.ReadInt32(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16()
            };
        }

        private GzfEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new GzfEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private GzfEntry ReadEntry(BinaryReaderX reader)
        {
            return new GzfEntry
            {
                codePoint = reader.ReadInt32(),
                charWidth = reader.ReadInt16(),
                imageIndex = reader.ReadInt16(),
                posX = reader.ReadInt16(),
                column = reader.ReadByte(),
                row = reader.ReadByte()
            };
        }

        private void WriteHeader(GzfHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.imgInfoOffset);
            writer.Write(header.imgInfoSize);
            writer.Write(header.entrySize);
            writer.Write(header.imgCount);
            writer.Write(header.entryCount);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
            writer.Write(header.format);
            writer.Write(header.unk4);
            writer.Write(header.unk5);
            writer.Write(header.glyphWidth);
            writer.Write(header.glyphHeight);
        }

        private void WriteImageInfos(IList<GzfImageInfo> imageInfos, BinaryWriterX writer)
        {
            foreach (GzfImageInfo imageInfo in imageInfos)
                WriteImageInfo(imageInfo, writer);
        }

        private void WriteImageInfo(GzfImageInfo imageInfo, BinaryWriterX writer)
        {
            writer.Write(imageInfo.offset);
            writer.Write(imageInfo.width);
            writer.Write(imageInfo.height);
        }

        private void WriteEntries(IList<GzfEntry> entries, BinaryWriterX writer)
        {
            foreach (GzfEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(GzfEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.codePoint);
            writer.Write(entry.charWidth);
            writer.Write(entry.imageIndex);
            writer.Write(entry.posX);
            writer.Write(entry.column);
            writer.Write(entry.row);
        }
    }
}
