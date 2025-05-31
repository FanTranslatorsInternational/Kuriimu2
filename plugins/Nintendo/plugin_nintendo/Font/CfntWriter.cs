using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Plugin.File.Image;
using plugin_nintendo.Font.DataClasses;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace plugin_nintendo.Font
{
    class CfntWriter
    {
        private const int HeaderSize_ = 0x14;
        private const int InfoOffset_ = HeaderSize_;
        private const int InfoSize_ = 0x18;
        private const int ImageOffset_ = InfoOffset_ + InfoSize_ + 8;
        private const int ImageDataOffset_ = 0x80;

        private readonly CfntEncodingProvider _encodingProvider = new();
        private readonly CodeRangeOptimalParser _rangeOptimalParser = new();

        public void Write(Stream output, CfntData fontData)
        {
            CharacterInfo[] characters = fontData.Characters.OrderBy(x => x.CodePoint).ToArray();

            // Create header
            CfntHeader header = CreateHeader(fontData.MetaData);

            // Create info section
            CfntInfSection infoSection = CreateInfoSection(fontData.MetaData);

            // Create image section
            CfntTglpSection imageSection = CreateImageSection(characters, fontData.MetaData, fontData.ImageData);

            // Create mapping sections
            CfntCmapSection[] mappingSections = CreateMappingSections(characters);

            // Create widths section
            CfntCwdhSection widthSection = CreateWidthSection(characters);

            using var writer = new BinaryWriterX(output, true);

            // Write image section
            writer.BaseStream.Position = ImageOffset_;

            imageSection.sheetDataOffset = ImageDataOffset_;

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
            infoSection.tglpOffset = ImageOffset_ + 8;
            infoSection.cwdhOffset = widthOffset + 8;
            infoSection.cmapOffset = mappingOffset + 8;

            WriteInfoSection(infoSection, writer);

            // Write header
            writer.BaseStream.Position = 0;

            header.fileSize = (int)output.Length;
            header.blockCount = mappingSections.Length + 3;

            WriteHeader(header, writer);
        }

        private CfntHeader CreateHeader(CfntMetaData metaData)
        {
            return new CfntHeader
            {
                magic = "CFNT",
                endianess = 0xfeff,
                headerSize = HeaderSize_,
                version = metaData.Version
            };
        }

        private CfntInfSection CreateInfoSection(CfntMetaData metaData)
        {
            return new CfntInfSection
            {
                fontType = metaData.Type,
                lineFeed = metaData.LineFeed,
                defaultWidths = metaData.DefaultWidths,
                encoding = metaData.Encoding,
                width = metaData.Width,
                height = metaData.Height,
                ascent = metaData.Ascent,
                reserved = 0
            };
        }

        private CfntTglpSection CreateImageSection(CharacterInfo[] characters, CfntMetaData metaData, CfntImageData imageData)
        {
            int maxCharSize = characters.Max(x => x.BoundingBox.Width);

            Size cellSize = GetCellSize(characters);
            ImageFile[] sheets = CreateSheetImages(characters, imageData.ImageFormat, cellSize, imageData.SheetSize, out int columnCount, out int rowCount);

            int imageFormat = sheets[0].ImageInfo.ImageFormat;

            var sheetData = new byte[sheets.Length][];
            for (var i = 0; i < sheets.Length; i++)
                sheetData[i] = sheets[i].ImageInfo.ImageData;

            return new CfntTglpSection
            {
                cellWidth = (byte)cellSize.Width,
                cellHeight = (byte)cellSize.Height,
                baseline = metaData.Baseline,
                maxCharWidth = (byte)maxCharSize,
                sheetSize = sheets[0].ImageInfo.ImageData.Length,
                sheetCount = (short)sheets.Length,
                sheetFormat = (short)imageFormat,
                columnCount = (short)columnCount,
                rowCount = (short)rowCount,
                sheetWidth = (short)sheets[0].ImageInfo.ImageSize.Width,
                sheetHeight = (short)sheets[0].ImageInfo.ImageSize.Height,
                sheetData = sheetData
            };
        }

        private CfntCmapSection[] CreateMappingSections(CharacterInfo[] characters)
        {
            var directMappingSections = new List<CfntCmapSection>();
            var indirectMappingSections = new List<CfntCmapSection>();
            var scanEntries = new List<CfntCmapScanTableEntry>();

            var previousEnd = 0;
            foreach (Match match in _rangeOptimalParser.Parse(characters))
            {
                // Add raw entries
                for (int i = previousEnd; i < match.Start; i++)
                {
                    scanEntries.Add(new CfntCmapScanTableEntry
                    {
                        code = characters[i].CodePoint,
                        index = (ushort)i
                    });
                }

                // Add code range section
                var section = new CfntCmapSection
                {
                    codeBegin = characters[match.Start].CodePoint,
                    codeEnd = characters[match.End].CodePoint,
                    mappingMethod = (short)match.Method
                };

                switch (match.Method)
                {
                    case 0:
                        section.indexData = new CfntCmapIndexEntry
                        {
                            index = (short)match.Start
                        };
                        directMappingSections.Add(section);
                        break;

                    case 1:
                        var tableEntries = new List<CfntCmapIndexEntry>();
                        for (int i = match.Start; i <= match.End; i++)
                        {
                            tableEntries.Add(new CfntCmapIndexEntry
                            {
                                index = (short)i
                            });

                            if (i + 1 > match.End)
                                break;

                            for (int j = characters[i].CodePoint + 1; j < characters[i + 1].CodePoint; j++)
                            {
                                tableEntries.Add(new CfntCmapIndexEntry
                                {
                                    index = -1
                                });
                            }
                        }

                        section.indexData = new CfntCmapTableEntry
                        {
                            indexes = [.. tableEntries]
                        };
                        indirectMappingSections.Add(section);
                        break;
                }

                previousEnd = match.End + 1;
            }

            var result = new List<CfntCmapSection>();
            result.AddRange(directMappingSections);
            result.AddRange(indirectMappingSections);
            result.Add(new CfntCmapSection
            {
                codeBegin = 0x0000,
                codeEnd = 0xFFFF,
                mappingMethod = 2,
                indexData = new CfntCmapScanTable
                {
                    entryCount = (ushort)scanEntries.Count,
                    entries = [.. scanEntries]
                }
            });

            return [.. result];
        }

        private CfntCwdhSection CreateWidthSection(CharacterInfo[] characters)
        {
            var widths = new CfntCwdhEntry[characters.Length];
            for (var i = 0; i < characters.Length; i++)
            {
                widths[i] = new CfntCwdhEntry
                {
                    charWidth = (sbyte)characters[i].BoundingBox.Width,
                    glyphWidth = (sbyte)(characters[i].Glyph?.Width ?? 0),
                    leftPadding = (sbyte)characters[i].GlyphPosition.X
                };

            }

            return new CfntCwdhSection
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

        private ImageFile[] CreateSheetImages(CharacterInfo[] characters, int imageFormat, Size cellSize, Size sheetSize, out int columnCount, out int rowCount)
        {
            columnCount = sheetSize.Width / (cellSize.Width + 1);
            rowCount = sheetSize.Height / (cellSize.Height + 1);
            var sheetCount = (int)Math.Ceiling(characters.Length / (float)(rowCount * columnCount));

            var result = new ImageFile[sheetCount];

            var characterIndex = 0;
            EncodingDefinition encodingDefinition = _encodingProvider.GetEncodingDefinitions();
            for (var i = 0; i < sheetCount; i++)
            {
                var sheetImage = new Image<Rgba32>(sheetSize.Width, sheetSize.Height);
                for (var y = 0; y < rowCount; y++)
                {
                    for (var x = 0; x < columnCount; x++)
                    {
                        if (characterIndex >= characters.Length)
                            break;

                        CharacterInfo character = characters[characterIndex++];

                        if (character.Glyph is null)
                            continue;

                        var glyphPosition = new Point(x * (cellSize.Width + 1) + 1, character.GlyphPosition.Y + y * (cellSize.Height + 1) + 1);
                        sheetImage.Mutate(config => config.DrawImage(character.Glyph, glyphPosition, 1f));
                    }

                    if (characterIndex >= characters.Length)
                        break;
                }

                var sheetImageFile = ImageFile.Create(sheetSize, encodingDefinition);
                sheetImageFile.ImageInfo.ImageFormat = imageFormat;
                sheetImageFile.ImageInfo.RemapPixels = context => new CtrSwizzle(context);

                sheetImageFile.SetImage(sheetImage);

                result[i] = sheetImageFile;
            }

            return result;
        }

        private void WriteHeader(CfntHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.endianess);
            writer.Write(header.headerSize);
            writer.Write(header.version);
            writer.Write(header.fileSize);
            writer.Write(header.blockCount);
        }

        private void WriteInfoSection(CfntInfSection infoSection, BinaryWriterX writer)
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
            writer.Write(infoSection.tglpOffset);
            writer.Write(infoSection.cwdhOffset);
            writer.Write(infoSection.cmapOffset);
            writer.Write(infoSection.height);
            writer.Write(infoSection.width);
            writer.Write(infoSection.ascent);
            writer.Write(infoSection.reserved);

            long endOffset = writer.BaseStream.Position;

            writer.BaseStream.Position = baseOffset;

            writer.WriteString("FINF", writeNullTerminator: false);
            writer.Write((int)(endOffset - baseOffset));

            writer.BaseStream.Position = endOffset;
        }

        private void WriteImageSection(CfntTglpSection imageSection, BinaryWriterX writer)
        {
            long baseOffset = writer.BaseStream.Position;

            writer.BaseStream.Position = baseOffset + 8;

            writer.Write(imageSection.cellWidth);
            writer.Write(imageSection.cellHeight);
            writer.Write(imageSection.baseline);
            writer.Write(imageSection.maxCharWidth);
            writer.Write(imageSection.sheetSize);
            writer.Write(imageSection.sheetCount);
            writer.Write(imageSection.sheetFormat);
            writer.Write(imageSection.columnCount);
            writer.Write(imageSection.rowCount);
            writer.Write(imageSection.sheetWidth);
            writer.Write(imageSection.sheetHeight);
            writer.Write(imageSection.sheetDataOffset);

            writer.BaseStream.Position = imageSection.sheetDataOffset;

            foreach (byte[] sheetData in imageSection.sheetData)
                writer.Write(sheetData);

            long endOffset = writer.BaseStream.Position;

            writer.BaseStream.Position = baseOffset;

            writer.WriteString("TGLP", writeNullTerminator: false);
            writer.Write((int)(endOffset - baseOffset));

            writer.BaseStream.Position = endOffset;
        }

        private void WriteWidthSection(CfntCwdhSection widthSection, BinaryWriterX writer)
        {
            long baseOffset = writer.BaseStream.Position;

            writer.BaseStream.Position = baseOffset + 8;

            writer.Write(widthSection.startIndex);
            writer.Write(widthSection.endIndex);
            writer.Write(widthSection.nextCwdhOffset);

            foreach (CfntCwdhEntry entry in widthSection.entries)
            {
                writer.Write(entry.leftPadding);
                writer.Write(entry.glyphWidth);
                writer.Write(entry.charWidth);
            }

            writer.WriteAlignment(4);

            long endOffset = writer.BaseStream.Position;

            writer.BaseStream.Position = baseOffset;

            writer.WriteString("CWDH", writeNullTerminator: false);
            writer.Write((int)(endOffset - baseOffset));

            writer.BaseStream.Position = endOffset;
        }

        private void WriteMappingSections(CfntCmapSection[] mappingSections, BinaryWriterX writer)
        {
            for (var i = 0; i < mappingSections.Length; i++)
                WriteMappingSection(mappingSections[i], i + 1 >= mappingSections.Length, writer);
        }

        private void WriteMappingSection(CfntCmapSection mappingSection, bool isLast, BinaryWriterX writer)
        {
            long baseOffset = writer.BaseStream.Position;

            // Write section index data
            writer.BaseStream.Position = baseOffset + 0x14;

            switch (mappingSection.mappingMethod)
            {
                case 0:
                    WriteCmapIndexEntry((CfntCmapIndexEntry)mappingSection.indexData, writer);
                    break;

                case 1:
                    WriteCmapTableEntry((CfntCmapTableEntry)mappingSection.indexData, writer);
                    break;

                case 2:
                    WriteCmapScanTable((CfntCmapScanTable)mappingSection.indexData, writer);
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

            writer.WriteString("CMAP", writeNullTerminator: false);
            writer.Write((int)(endOffset - baseOffset));

            writer.BaseStream.Position = endOffset;
        }

        private void WriteCmapIndexEntry(CfntCmapIndexEntry indexData, BinaryWriterX writer)
        {
            writer.Write(indexData.index);
        }

        private void WriteCmapTableEntry(CfntCmapTableEntry tableData, BinaryWriterX writer)
        {
            foreach (CfntCmapIndexEntry index in tableData.indexes)
                WriteCmapIndexEntry(index, writer);
        }

        private void WriteCmapScanTable(CfntCmapScanTable scanData, BinaryWriterX writer)
        {
            writer.Write(scanData.entryCount);

            foreach (CfntCmapScanTableEntry entry in scanData.entries)
            {
                writer.Write(entry.code);
                writer.Write(entry.index);
            }
        }
    }
}
