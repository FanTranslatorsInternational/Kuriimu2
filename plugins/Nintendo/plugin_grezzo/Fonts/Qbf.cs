using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Generation;
using Kanvas.Contract.Encoding;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace plugin_grezzo.Fonts
{
    class Qbf
    {
        private QbfHeader _header;

        public List<CharacterInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            _header = ReadHeader(br);

            QbfEntry[] entries = ReadEntries(br, _header.entryCount);

            int glyphLength = _header.bitsPerPixel * _header.glyphWidth * _header.glyphHeight / 8;
            byte[][] glyphs = ReadGlyphs(br, _header.glyphCount, glyphLength);

            EncodingDefinition encodingDefinition = QbfSupport.GetEncodingDefinition();

            var characters = new List<CharacterInfo>();
            foreach (QbfEntry entry in entries)
            {
                if (entry.unk3 != 0)
                {
                    characters.Add(new CharacterInfo
                    {
                        CodePoint = (char)entry.codePoint,
                        GlyphPosition = new Point(entry.posX, 0),
                        BoundingBox = new Size(entry.charWidth, _header.glyphHeight)
                    });
                    continue;
                }

                IColorEncoding encoding = QbfSupport.Formats[_header.imgFormat];
                var imageInfo = new ImageFileInfo
                {
                    BitDepth = encoding.BitDepth,
                    ImageData = glyphs[entry.index],
                    ImageFormat = _header.imgFormat,
                    ImageSize = new Size(_header.glyphWidth, _header.glyphHeight)
                };

                Image<Rgba32> rawGlyph = ImageFile.Decode(imageInfo, encodingDefinition);
                GlyphDescriptionData glyphDescription = WhiteSpaceMeasurer.MeasureWhiteSpace(rawGlyph);

                if (glyphDescription.Size is { Width: > 0, Height: > 0 })
                {
                    characters.Add(new CharacterInfo
                    {
                        CodePoint = (char)entry.codePoint,
                        GlyphPosition = new Point(glyphDescription.Position.X - entry.posX, glyphDescription.Position.Y),
                        BoundingBox = new Size(entry.charWidth, _header.glyphHeight - glyphDescription.Position.Y),
                        Glyph = rawGlyph.Clone(context => context.Crop(new Rectangle(glyphDescription.Position, glyphDescription.Size)))
                    });
                }
                else if (entry.charWidth > 0)
                {
                    characters.Add(new CharacterInfo
                    {
                        CodePoint = (char)entry.codePoint,
                        GlyphPosition = Point.Empty,
                        BoundingBox = new Size(entry.charWidth, _header.glyphHeight),
                        Glyph = rawGlyph.Clone(context => context.Crop(new Rectangle(entry.posX, 0, entry.charWidth, _header.glyphHeight)))
                    });
                }
                else
                {
                    characters.Add(new CharacterInfo
                    {
                        CodePoint = (char)entry.codePoint,
                        GlyphPosition = new Point(entry.posX, 0),
                        BoundingBox = new Size(entry.charWidth, _header.glyphHeight),
                        Glyph = null
                    });
                }
            }

            return characters;
        }

        public void Save(Stream output, List<CharacterInfo> characters)
        {
            using var bw = new BinaryWriterX(output);

            List<QbfEntry> entries = CreateEntries(characters, out List<byte[]> glyphs);

            _header.entryCount = (short)entries.Count;
            _header.glyphCount = (short)glyphs.Count;

            WriteHeader(_header, bw);
            WriteEntries(entries, bw);
            WriteGlyphs(glyphs, bw);
        }

        private List<QbfEntry> CreateEntries(List<CharacterInfo> characters, out List<byte[]> glyphs)
        {
            glyphs = [];

            _header.glyphWidth = (byte)((characters.Max(x => x.Glyph?.Width ?? 0) + 7) & ~7);
            _header.glyphHeight = (byte)((characters.Max(x => x.Glyph?.Height ?? 0) + 7) & ~7);

            EncodingDefinition encodingDefinition = QbfSupport.GetEncodingDefinition();

            var result = new List<QbfEntry>();

            var emptyIndex = 0;
            foreach (CharacterInfo character in characters.OrderBy(x => x.CodePoint))
            {
                if (character.Glyph is null)
                {
                    result.Add(new QbfEntry
                    {
                        codePoint = (short)character.CodePoint,
                        index = (short)emptyIndex++,
                        posX = (byte)character.GlyphPosition.X,
                        charWidth = (byte)character.BoundingBox.Width,
                        unk3 = 1
                    });
                }
                else
                {
                    int leftPos = (_header.glyphWidth - character.Glyph.Width) / 2;

                    result.Add(new QbfEntry
                    {
                        codePoint = (short)character.CodePoint,
                        index = (short)glyphs.Count,
                        posX = (byte)(leftPos - character.GlyphPosition.X),
                        charWidth = (byte)character.BoundingBox.Width,
                        unk3 = 0
                    });

                    var glyph = new Image<Rgba32>(_header.glyphWidth, _header.glyphHeight);
                    glyph.Mutate(context => context.DrawImage(character.Glyph, new Point(leftPos, character.GlyphPosition.Y), 1f));

                    IColorEncoding encoding = QbfSupport.Formats[_header.imgFormat];
                    var imageInfo = new ImageFileInfo
                    {
                        BitDepth = encoding.BitDepth,
                        ImageData = [],
                        ImageFormat = _header.imgFormat,
                        ImageSize = Size.Empty
                    };

                    ImageFile.Encode(glyph, imageInfo, encodingDefinition);

                    glyphs.Add(imageInfo.ImageData);
                }
            }

            return result;
        }

        private QbfHeader ReadHeader(BinaryReaderX reader)
        {
            return new QbfHeader
            {
                magic = reader.ReadString(4),
                entryCount = reader.ReadInt16(),
                glyphCount = reader.ReadInt16(),
                unk2 = reader.ReadInt32(),
                bitsPerPixel = reader.ReadByte(),
                glyphWidth = reader.ReadByte(),
                glyphHeight = reader.ReadByte(),
                imgFormat = reader.ReadByte()
            };
        }

        private QbfEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new QbfEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private QbfEntry ReadEntry(BinaryReaderX reader)
        {
            return new QbfEntry
            {
                codePoint = reader.ReadInt16(),
                index = reader.ReadInt16(),
                posX = reader.ReadByte(),
                charWidth = reader.ReadByte(),
                unk3 = reader.ReadInt16()
            };
        }

        private byte[][] ReadGlyphs(BinaryReaderX reader, int count, int glyphLength)
        {
            var result = new byte[count][];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadBytes(glyphLength);

            return result;
        }

        private void WriteHeader(QbfHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.entryCount);
            writer.Write(header.glyphCount);
            writer.Write(header.unk2);
            writer.Write(header.bitsPerPixel);
            writer.Write(header.glyphWidth);
            writer.Write(header.glyphHeight);
            writer.Write(header.imgFormat);
        }

        private void WriteEntries(IList<QbfEntry> entries, BinaryWriterX writer)
        {
            foreach (QbfEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(QbfEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.codePoint);
            writer.Write(entry.index);
            writer.Write(entry.posX);
            writer.Write(entry.charWidth);
            writer.Write(entry.unk3);
        }

        private void WriteGlyphs(IList<byte[]> glyphs, BinaryWriterX writer)
        {
            foreach (byte[] glyph in glyphs)
                writer.Write(glyph);
        }
    }
}
