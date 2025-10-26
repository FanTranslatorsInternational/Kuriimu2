using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Plugin.File.Image;
using plugin_nintendo.Font.DataClasses;
using plugin_nintendo.Font.DataClasses.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace plugin_nintendo.Font
{
    class NftrWriter
    {
        private const int HeaderSize_ = 0x10;
        private const int InfoOffset_ = HeaderSize_;
        private const int InfoSize_ = 0x1C;
        private const int ExtendedInfoSize_ = 0x20;
        private const int ImageOffset_ = InfoOffset_ + InfoSize_;
        private const int ExtendedImageOffset_ = InfoOffset_ + ExtendedInfoSize_;

        private readonly NftrEncodingProvider _encodingProvider = new();
        private readonly CodeRangeOptimalParser _rangeOptimalParser = new();

        public void Write(Stream output, NftrData fontData)
        {
            CharacterInfo[] characters = fontData.Characters.OrderBy(x => x.CodePoint).ToArray();

            // Create header
            NftrHeader header = CreateHeader(fontData.MetaData);

            // Create info section
            NftrInfSection infoSection = CreateInfoSection(fontData.MetaData);

            // Create image section
            NftrCglpSection imageSection = CreateImageSection(characters, fontData.MetaData, fontData.ImageData);

            // Create mapping sections
            CmapSection[] mappingSections = CreateMappingSections(characters);

            // Create widths section
            CwdhSection widthSection = CreateWidthSection(characters);

            using var writer = new BinaryWriterX(output, true);

            // Write image section
            writer.BaseStream.Position = infoSection.hasExtendedData ? ExtendedImageOffset_ : ImageOffset_;

            WriteImageSection(imageSection, writer);

            // Write width section
            var widthOffset = (int)writer.BaseStream.Position;

            WriteWidthSection(widthSection, writer);

            // Write mapping sections
            var mappingOffset = (int)writer.BaseStream.Position;

            WriteMappingSections(mappingSections, writer);

            // Write info section
            writer.BaseStream.Position = InfoOffset_;

            infoSection.fallbackCharIndex = 0;
            infoSection.cglpOffset = ImageOffset_ + 8;
            infoSection.cwdhOffset = widthOffset + 8;
            infoSection.cmapOffset = mappingOffset + 8;

            WriteInfoSection(infoSection, writer);

            // Write header
            writer.BaseStream.Position = 0;

            header.fileSize = (int)output.Length;
            header.blockCount = (short)(mappingSections.Length + 3);

            WriteHeader(header, writer);
        }

        private NftrHeader CreateHeader(NftrMetaData metaData)
        {
            return new NftrHeader
            {
                magic = "RTFN",
                endianess = 0xfeff,
                version = metaData.Version,
                infoOffset = InfoOffset_
            };
        }

        private NftrInfSection CreateInfoSection(NftrMetaData metaData)
        {
            return new NftrInfSection
            {
                fontType = metaData.Type,
                lineFeed = metaData.LineFeed,
                defaultWidths = metaData.DefaultWidths,
                encoding = metaData.Encoding,
                width = metaData.Width,
                height = metaData.Height,
                bearingX = metaData.BearingX,
                bearingY = metaData.BearingY,
                hasExtendedData = metaData.HasExtendedData
            };
        }

        private NftrCglpSection CreateImageSection(CharacterInfo[] characters, NftrMetaData metaData, NftrImageData imageData)
        {
            int maxCharSize = characters.Max(x => x.BoundingBox.Width);

            Size cellSize = GetCellSize(characters);
            byte[][] glyphData = CreateGlyphData(characters, imageData.BitDepth, cellSize);

            return new NftrCglpSection
            {
                cellWidth = (byte)cellSize.Width,
                cellHeight = (byte)cellSize.Height,
                cellSize = (short)glyphData[0].Length,
                baseline = metaData.Baseline,
                maxCharWidth = (byte)maxCharSize,
                cellBitDepth = imageData.BitDepth,
                cellRotation = imageData.Rotation,
                cellData = glyphData
            };
        }

        private CmapSection[] CreateMappingSections(CharacterInfo[] characters)
        {
            var directMappingSections = new List<CmapSection>();
            var indirectMappingSections = new List<CmapSection>();
            var scanEntries = new List<CmapScanTableEntry>();

            var previousEnd = 0;
            foreach (Match match in _rangeOptimalParser.Parse(characters))
            {
                // Add raw entries
                for (int i = previousEnd; i < match.Start; i++)
                {
                    scanEntries.Add(new CmapScanTableEntry
                    {
                        code = characters[i].CodePoint,
                        index = (ushort)i
                    });
                }

                // Add code range section
                var section = new CmapSection
                {
                    codeBegin = characters[match.Start].CodePoint,
                    codeEnd = characters[match.End].CodePoint,
                    mappingMethod = (short)match.Method
                };

                switch (match.Method)
                {
                    case 0:
                        section.indexData = new CmapIndexEntry
                        {
                            index = (short)match.Start
                        };
                        directMappingSections.Add(section);
                        break;

                    case 1:
                        var tableEntries = new List<CmapIndexEntry>();
                        for (int i = match.Start; i <= match.End; i++)
                        {
                            tableEntries.Add(new CmapIndexEntry
                            {
                                index = (short)i
                            });

                            if (i + 1 > match.End)
                                break;

                            for (int j = characters[i].CodePoint + 1; j < characters[i + 1].CodePoint; j++)
                            {
                                tableEntries.Add(new CmapIndexEntry
                                {
                                    index = -1
                                });
                            }
                        }

                        section.indexData = new CmapTableEntry
                        {
                            indexes = [.. tableEntries]
                        };
                        indirectMappingSections.Add(section);
                        break;
                }

                previousEnd = match.End + 1;
            }

            var result = new List<CmapSection>();
            result.AddRange(directMappingSections);
            result.AddRange(indirectMappingSections);
            result.Add(new CmapSection
            {
                codeBegin = 0x0000,
                codeEnd = 0xFFFF,
                mappingMethod = 2,
                indexData = new CmapScanTable
                {
                    entryCount = (ushort)scanEntries.Count,
                    entries = [.. scanEntries]
                }
            });

            return [.. result];
        }

        private CwdhSection CreateWidthSection(CharacterInfo[] characters)
        {
            var widths = new CwdhEntry[characters.Length];
            for (var i = 0; i < characters.Length; i++)
            {
                widths[i] = new CwdhEntry
                {
                    charWidth = (sbyte)characters[i].BoundingBox.Width,
                    glyphWidth = (sbyte)(characters[i].Glyph?.Width ?? 0),
                    leftPadding = (sbyte)characters[i].GlyphPosition.X
                };

            }

            return new CwdhSection
            {
                startIndex = 0,
                endIndex = (short)(characters.Length - 1),
                entries = widths
            };
        }

        private Size GetCellSize(CharacterInfo[] characters)
        {
            int cellWidth = characters.Max(x => x.Glyph?.Width ?? 0);
            int cellHeight = characters.Max(x => (x.Glyph?.Height ?? 0) + x.GlyphPosition.Y);

            return new Size(cellWidth, cellHeight);
        }

        private byte[][] CreateGlyphData(CharacterInfo[] characters, int bitDepth, Size cellSize)
        {
            var result = new byte[characters.Length][];

            int cellByteSize = ((cellSize.Width * cellSize.Height * bitDepth + 7) & ~7) / 8;
            EncodingDefinition encodingDefinition = _encodingProvider.GetEncodingDefinitions();

            for (var i = 0; i < characters.Length; i++)
            {
                CharacterInfo character = characters[i];

                if (character.Glyph is null)
                {
                    result[i] = new byte[cellByteSize];
                    continue;
                }

                var glyphImage = new Image<Rgba32>(cellSize.Width, cellSize.Height);

                var glyphPosition = new Point(0, character.GlyphPosition.Y);
                glyphImage.Mutate(config => config.DrawImage(character.Glyph, glyphPosition, 1f));

                var glyphImageFile = ImageFile.Create(cellSize, encodingDefinition);
                glyphImageFile.ImageInfo.ImageFormat = bitDepth;

                glyphImageFile.SetImage(glyphImage);

                result[i] = glyphImageFile.ImageInfo.ImageData;
            }

            return result;
        }

        private void WriteHeader(NftrHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.endianess);
            writer.Write(header.version);
            writer.Write(header.fileSize);
            writer.Write(header.infoOffset);
            writer.Write(header.blockCount);
        }

        private void WriteInfoSection(NftrInfSection infoSection, BinaryWriterX writer)
        {
            long baseOffset = writer.BaseStream.Position;

            writer.BaseStream.Position = baseOffset + 8;

            writer.Write(infoSection.fontType);
            writer.Write(infoSection.lineFeed);
            writer.Write(infoSection.fallbackCharIndex);
            writer.Write(infoSection.defaultWidths.leftPadding);
            writer.Write(infoSection.defaultWidths.glyphWidth);
            writer.Write(infoSection.defaultWidths.charWidth);
            writer.Write(infoSection.encoding);
            writer.Write(infoSection.cglpOffset);
            writer.Write(infoSection.cwdhOffset);
            writer.Write(infoSection.cmapOffset);

            if (infoSection.hasExtendedData)
            {
                writer.Write(infoSection.height);
                writer.Write(infoSection.width);
                writer.Write(infoSection.bearingX);
                writer.Write(infoSection.bearingY);
            }

            long endOffset = writer.BaseStream.Position;

            writer.BaseStream.Position = baseOffset;

            writer.WriteString("FNIF", writeNullTerminator: false);
            writer.Write((int)(endOffset - baseOffset));

            writer.BaseStream.Position = endOffset;
        }

        private void WriteImageSection(NftrCglpSection imageSection, BinaryWriterX writer)
        {
            long baseOffset = writer.BaseStream.Position;

            writer.BaseStream.Position = baseOffset + 8;

            writer.Write(imageSection.cellWidth);
            writer.Write(imageSection.cellHeight);
            writer.Write(imageSection.cellSize);
            writer.Write(imageSection.baseline);
            writer.Write(imageSection.maxCharWidth);
            writer.Write(imageSection.cellBitDepth);
            writer.Write(imageSection.cellRotation);

            foreach (byte[] cellData in imageSection.cellData)
                writer.Write(cellData);

            writer.WriteAlignment(4);

            long endOffset = writer.BaseStream.Position;

            writer.BaseStream.Position = baseOffset;

            writer.WriteString("PLGC", writeNullTerminator: false);
            writer.Write((int)(endOffset - baseOffset));

            writer.BaseStream.Position = endOffset;
        }

        private void WriteWidthSection(CwdhSection widthSection, BinaryWriterX writer)
        {
            long baseOffset = writer.BaseStream.Position;

            writer.BaseStream.Position = baseOffset + 8;

            writer.Write(widthSection.startIndex);
            writer.Write(widthSection.endIndex);
            writer.Write(widthSection.nextCwdhOffset);

            foreach (CwdhEntry entry in widthSection.entries)
            {
                writer.Write(entry.leftPadding);
                writer.Write(entry.glyphWidth);
                writer.Write(entry.charWidth);
            }

            writer.WriteAlignment(4);

            long endOffset = writer.BaseStream.Position;

            writer.BaseStream.Position = baseOffset;

            writer.WriteString("HDWC", writeNullTerminator: false);
            writer.Write((int)(endOffset - baseOffset));

            writer.BaseStream.Position = endOffset;
        }

        private void WriteMappingSections(CmapSection[] mappingSections, BinaryWriterX writer)
        {
            for (var i = 0; i < mappingSections.Length; i++)
                WriteMappingSection(mappingSections[i], i + 1 >= mappingSections.Length, writer);
        }

        private void WriteMappingSection(CmapSection mappingSection, bool isLast, BinaryWriterX writer)
        {
            long baseOffset = writer.BaseStream.Position;

            // Write section index data
            writer.BaseStream.Position = baseOffset + 0x14;

            switch (mappingSection.mappingMethod)
            {
                case 0:
                    WriteCmapIndexEntry((CmapIndexEntry)mappingSection.indexData, writer);
                    break;

                case 1:
                    WriteCmapTableEntry((CmapTableEntry)mappingSection.indexData, writer);
                    break;

                case 2:
                    WriteCmapScanTable((CmapScanTable)mappingSection.indexData, writer);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported cmap method {mappingSection.mappingMethod}.");
            }

            writer.WriteAlignment(4);

            long endOffset = writer.BaseStream.Position;

            mappingSection.nextCmapOffset = isLast ? 0 : (int)(endOffset + 8);

            // Write section header data
            writer.BaseStream.Position = baseOffset + 8;

            writer.Write(mappingSection.codeBegin);
            writer.Write(mappingSection.codeEnd);
            writer.Write(mappingSection.mappingMethod);
            writer.Write(mappingSection.reserved);
            writer.Write(mappingSection.nextCmapOffset);

            // Write NW4C header
            writer.BaseStream.Position = baseOffset;

            writer.WriteString("PAMC", writeNullTerminator: false);
            writer.Write((int)(endOffset - baseOffset));

            writer.BaseStream.Position = endOffset;
        }

        private void WriteCmapIndexEntry(CmapIndexEntry indexData, BinaryWriterX writer)
        {
            writer.Write(indexData.index);
        }

        private void WriteCmapTableEntry(CmapTableEntry tableData, BinaryWriterX writer)
        {
            foreach (CmapIndexEntry index in tableData.indexes)
                WriteCmapIndexEntry(index, writer);
        }

        private void WriteCmapScanTable(CmapScanTable scanData, BinaryWriterX writer)
        {
            writer.Write(scanData.entryCount);

            foreach (CmapScanTableEntry entry in scanData.entries)
            {
                writer.Write(entry.code);
                writer.Write(entry.index);
            }
        }
    }
}
