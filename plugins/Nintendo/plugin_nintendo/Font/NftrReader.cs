using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Generation;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using plugin_nintendo.Font.DataClasses;
using plugin_nintendo.Font.DataClasses.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ByteOrder = Komponent.Contract.Enums.ByteOrder;

namespace plugin_nintendo.Font
{
    class NftrReader
    {
        private readonly NftrEncodingProvider _encodingProvider = new();

        public NftrData Read(Stream input)
        {
            using var reader = new BinaryReaderX(input, true);
            reader.ByteOrder = PeekByteOrder(reader);

            NftrHeader header = ReadHeader(reader);
            Nw4cSection[] sections = ReadSections(reader, header.blockCount);

            // Get widths
            NftrInfSection infoSection = GetInfoSection(sections);
            CwdhSection[] widthSections = GetWidthSections(sections, infoSection.cwdhOffset);

            CwdhEntry[] widthEntries = GetWidths(infoSection, widthSections);

            // Get character codes
            CmapSection[] codeSections = GetCodeSections(sections, infoSection.cmapOffset);
            Dictionary<ushort, int> codes = GetCodes(codeSections);

            // Create glyphs
            NftrCglpSection imageSection = GetImageSection(sections);

            return new NftrData
            {
                Characters = GetCharacterInfos(imageSection, codes, widthEntries),
                MetaData = new NftrMetaData
                {
                    Version = header.version,
                    Type = infoSection.fontType,
                    Encoding = infoSection.encoding,
                    DefaultWidths = infoSection.defaultWidths,
                    LineFeed = infoSection.lineFeed,
                    Baseline = imageSection.baseline,
                    Width = infoSection.width,
                    Height = infoSection.height,
                    BearingX = infoSection.bearingX,
                    BearingY = infoSection.bearingY,
                    HasExtendedData = infoSection.hasExtendedData
                },
                ImageData = new NftrImageData
                {
                    BitDepth = imageSection.cellBitDepth,
                    Rotation = imageSection.cellRotation
                }
            };
        }

        private CwdhEntry[] GetWidths(NftrInfSection infoSection, CwdhSection[] widthSections)
        {
            int maxIndex = widthSections.Max(s => s.endIndex);
            var result = new CwdhEntry[maxIndex + 1];

            for (var i = 0; i < result.Length; i++)
                result[i] = infoSection.defaultWidths;

            foreach (CwdhSection widthSection in widthSections)
            {
                for (int i = widthSection.startIndex; i <= widthSection.endIndex; i++)
                    result[i] = widthSection.entries[i - widthSection.startIndex];
            }

            return result;
        }

        private Dictionary<ushort, int> GetCodes(CmapSection[] codeSections)
        {
            var result = new Dictionary<ushort, int>();

            foreach (CmapSection codeSection in codeSections)
            {
                switch (codeSection.mappingMethod)
                {
                    case 0:
                        var indexEntry = (CmapIndexEntry)codeSection.indexData;
                        for (ushort i = 0; i <= codeSection.codeEnd - codeSection.codeBegin; i++)
                        {
                            var code = (ushort)(codeSection.codeBegin + i);
                            result[code] = indexEntry.index + i;
                        }
                        break;

                    case 1:
                        var tableEntry = (CmapTableEntry)codeSection.indexData;
                        for (ushort i = 0; i <= codeSection.codeEnd - codeSection.codeBegin; i++)
                        {
                            if (tableEntry.indexes[i].index < 0)
                                continue;

                            var code = (ushort)(codeSection.codeBegin + i);
                            result[code] = tableEntry.indexes[i].index;
                        }
                        break;

                    case 2:
                        var scanEntry = (CmapScanTable)codeSection.indexData;
                        foreach (CmapScanTableEntry entry in scanEntry.entries)
                            result[entry.code] = entry.index;
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported cmap method {codeSection.mappingMethod}.");
                }
            }

            return result;
        }

        private List<CharacterInfo> GetCharacterInfos(NftrCglpSection imageSection, Dictionary<ushort, int> codes, CwdhEntry[] widthEntries)
        {
            var result = new List<CharacterInfo>(codes.Count);

            var index = 0;
            foreach (ushort code in codes.Keys.Order())
            {
                IImageFile glyphImage = GetGlyphImage(imageSection, imageSection.cellData[index++]);

                CwdhEntry widthEntry = widthEntries[codes[code]];

                var srcRect = new Rectangle(0, 0, widthEntry.glyphWidth, imageSection.cellHeight);

                Image<Rgba32> image = glyphImage.GetImage();
                GlyphDescriptionData glyphDescription = WhiteSpaceMeasurer.MeasureWhiteSpace(image, srcRect);
                Image<Rgba32>? glyph = glyphDescription.Size is { Width: > 0, Height: > 0 }
                    ? image.Clone(context => context.Crop(new Rectangle(glyphDescription.Position, glyphDescription.Size)))
                    : null;

                result.Add(new CharacterInfo
                {
                    CodePoint = (char)code,
                    GlyphPosition = new Point(widthEntry.leftPadding, glyphDescription.Position.Y - srcRect.Top),
                    BoundingBox = new Size(widthEntry.charWidth, imageSection.cellHeight),
                    Glyph = glyph
                });
            }

            return result;
        }

        private IImageFile GetGlyphImage(NftrCglpSection imageSection, byte[] cellData)
        {
            EncodingDefinition encodingDefinition = _encodingProvider.GetEncodingDefinitions();

            var imageInfo = new ImageFileInfo
            {
                BitDepth = imageSection.cellBitDepth,
                ImageData = cellData,
                ImageFormat = imageSection.cellBitDepth,
                ImageSize = new Size(imageSection.cellWidth, imageSection.cellHeight)
            };

            return new ImageFile(imageInfo, encodingDefinition);
        }

        private NftrCglpSection GetImageSection(Nw4cSection[] sections)
        {
            return (NftrCglpSection)sections.FirstOrDefault(x => x.magic is "PLGC").sectionData;
        }

        private NftrInfSection GetInfoSection(Nw4cSection[] sections)
        {
            return (NftrInfSection)sections.FirstOrDefault(x => x.magic is "FNIF").sectionData;
        }

        private CmapSection[] GetCodeSections(Nw4cSection[] sections, long mapSectionOffset)
        {
            var result = new List<CmapSection>();

            do
            {
                Nw4cSection section = sections.First(x => x.sectionOffset == mapSectionOffset);

                if (section.magic is not "PAMC")
                    break;

                var mapSection = (CmapSection)section.sectionData;
                mapSectionOffset = mapSection.nextCmapOffset;

                result.Add(mapSection);
            } while (mapSectionOffset > 0);

            return [.. result];
        }

        private CwdhSection[] GetWidthSections(Nw4cSection[] sections, long widthSectionOffset)
        {
            var result = new List<CwdhSection>();

            do
            {
                Nw4cSection section = sections.First(x => x.sectionOffset == widthSectionOffset);

                if (section.magic is not "HDWC")
                    break;

                var widthSection = (CwdhSection)section.sectionData;
                widthSectionOffset = widthSection.nextCwdhOffset;

                result.Add(widthSection);
            } while (widthSectionOffset > 0);

            return [.. result];
        }

        private ByteOrder PeekByteOrder(BinaryReaderX reader)
        {
            long bkPos = reader.BaseStream.Position;
            ByteOrder bkByteOrder = reader.ByteOrder;

            reader.BaseStream.Position = 4;
            reader.ByteOrder = ByteOrder.BigEndian;
            ushort byteOrderValue = reader.ReadUInt16();

            reader.BaseStream.Position = bkPos;
            reader.ByteOrder = bkByteOrder;

            return (ByteOrder)byteOrderValue;
        }

        private NftrHeader ReadHeader(BinaryReaderX reader)
        {
            return new NftrHeader
            {
                magic = reader.ReadString(4),
                endianess = reader.ReadUInt16(),
                version = reader.ReadUInt16(),
                fileSize = reader.ReadInt32(),
                infoOffset = reader.ReadInt16(),
                blockCount = reader.ReadInt16()
            };
        }

        private Nw4cSection[] ReadSections(BinaryReaderX reader, int sectionCount)
        {
            var result = new Nw4cSection[sectionCount];

            for (var i = 0; i < sectionCount; i++)
                result[i] = ReadSection(reader);

            return [.. result];
        }

        private Nw4cSection ReadSection(BinaryReaderX reader)
        {
            long sectionOffset = reader.BaseStream.Position + 8;
            string magic = reader.ReadString(4);
            int sectionSize = reader.ReadInt32();

            object sectionData;
            switch (magic)
            {
                case "FNIF":
                    sectionData = ReadInfSection(reader, sectionSize);
                    break;

                case "PLGC":
                    sectionData = ReadCglpSection(reader, sectionSize);
                    break;

                case "HDWC":
                    sectionData = ReadCwdhSection(reader);
                    break;

                case "PAMC":
                    sectionData = ReadCmapSection(reader);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported font section {magic}.");
            }

            return new Nw4cSection
            {
                sectionOffset = sectionOffset,
                magic = magic,
                sectionSize = sectionSize,
                sectionData = sectionData
            };
        }

        private NftrInfSection ReadInfSection(BinaryReaderX reader, int sectionSize)
        {
            var infSection = new NftrInfSection
            {
                fontType = reader.ReadByte(),
                lineFeed = reader.ReadByte(),
                fallbackCharIndex = reader.ReadUInt16(),
                defaultWidths = ReadCwdhEntry(reader),
                encoding = reader.ReadByte(),
                cglpOffset = reader.ReadInt32(),
                cwdhOffset = reader.ReadInt32(),
                cmapOffset = reader.ReadInt32()
            };

            if (sectionSize == 0x20)
            {
                infSection.height = reader.ReadByte();
                infSection.width = reader.ReadByte();
                infSection.bearingX = reader.ReadByte();
                infSection.bearingY = reader.ReadByte();
                infSection.hasExtendedData = true;
            }

            return infSection;
        }

        private NftrCglpSection ReadCglpSection(BinaryReaderX reader, int sectionSize)
        {
            var section = new NftrCglpSection
            {
                cellWidth = reader.ReadByte(),
                cellHeight = reader.ReadByte(),
                cellSize = reader.ReadInt16(),
                baseline = reader.ReadByte(),
                maxCharWidth = reader.ReadByte(),
                cellBitDepth = reader.ReadByte(),
                cellRotation = reader.ReadByte()
            };

            int tileCount = (sectionSize - 0x10) / section.cellSize;

            section.cellData = new byte[tileCount][];
            for (var i = 0; i < tileCount; i++)
                section.cellData[i] = reader.ReadBytes(section.cellSize);

            reader.SeekAlignment(4);

            return section;
        }

        private CwdhSection ReadCwdhSection(BinaryReaderX reader)
        {
            var section = new CwdhSection
            {
                startIndex = reader.ReadInt16(),
                endIndex = reader.ReadInt16(),
                nextCwdhOffset = reader.ReadInt32()
            };

            int entryCount = section.endIndex - section.startIndex + 1;

            section.entries = new CwdhEntry[entryCount];
            for (var i = 0; i < entryCount; i++)
                section.entries[i] = ReadCwdhEntry(reader);

            reader.SeekAlignment(4);

            return section;
        }

        private CwdhEntry ReadCwdhEntry(BinaryReaderX reader)
        {
            return new CwdhEntry
            {
                leftPadding = reader.ReadSByte(),
                glyphWidth = reader.ReadSByte(),
                charWidth = reader.ReadSByte()
            };
        }

        private CmapSection ReadCmapSection(BinaryReaderX reader)
        {
            var section = new CmapSection
            {
                codeBegin = reader.ReadUInt16(),
                codeEnd = reader.ReadUInt16(),
                mappingMethod = reader.ReadInt16(),
                reserved = reader.ReadInt16(),
                nextCmapOffset = reader.ReadInt32()
            };

            switch (section.mappingMethod)
            {
                case 0:
                    section.indexData = ReadCmapIndexEntry(reader);
                    break;

                case 1:
                    section.indexData = ReadCmapTableEntry(reader, section.codeEnd - section.codeBegin + 1);
                    break;

                case 2:
                    section.indexData = ReadCmapScanTable(reader);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported cmap method {section.mappingMethod}.");
            }

            reader.SeekAlignment(4);

            return section;
        }

        private CmapIndexEntry ReadCmapIndexEntry(BinaryReaderX reader)
        {
            return new CmapIndexEntry
            {
                index = reader.ReadInt16()
            };
        }

        private CmapTableEntry ReadCmapTableEntry(BinaryReaderX reader, int count)
        {
            var indexes = new CmapIndexEntry[count];
            for (var i = 0; i < count; i++)
                indexes[i] = ReadCmapIndexEntry(reader);

            return new CmapTableEntry
            {
                indexes = indexes
            };
        }

        private CmapScanTable ReadCmapScanTable(BinaryReaderX reader)
        {
            ushort entryCount = reader.ReadUInt16();

            var indexes = new CmapScanTableEntry[entryCount];
            for (var i = 0; i < entryCount; i++)
                indexes[i] = ReadCmapScanTableEntry(reader);

            return new CmapScanTable
            {
                entryCount = entryCount,
                entries = indexes
            };
        }

        private CmapScanTableEntry ReadCmapScanTableEntry(BinaryReaderX reader)
        {
            return new CmapScanTableEntry
            {
                code = reader.ReadUInt16(),
                index = reader.ReadUInt16()
            };
        }
    }
}
