using Kanvas;
using Komponent.IO;
using Konnect.Plugin.File.Image;

namespace plugin_level5.N3DS.Image
{
    #region Section structs

    class SectionHeader
    {
        public string magic;
        public int sectionSize;
        public int zero0;
        public int nextSectionOffset;
    }

    class MainSection
    {
        public SectionHeader Header { get; }

        public IReadOnlyList<Section> Sections { get; }

        public byte[] Data { get; set; }

        private MainSection(SectionHeader header, IReadOnlyList<Section> sections, byte[] data)
        {
            Header = header;
            Sections = sections;
            Data = data;
        }

        public static MainSection Read(BinaryReaderX br)
        {
            var startPosition = br.BaseStream.Position;
            var header = ReadSectionHeader(br);

            var sections = new List<Section>();
            do
            {
                sections.Add(Section.Read(br));
            } while (sections.Last().Header.nextSectionOffset != 0);

            byte[] data = null;
            if (br.BaseStream.Position - startPosition < header.sectionSize)
                data = br.ReadBytes((int)(header.sectionSize - (br.BaseStream.Position - startPosition)));

            return new MainSection(header, sections, data);
        }

        public int GetLength()
        {
            return 16 + Sections.Sum(x => x.GetLength()) + (Data?.Length ?? 0);
        }

        public Section GetSection(string magic)
        {
            return Sections.First(x => x.Header.magic == magic);
        }

        public void Write(BinaryWriterX bw)
        {
            var startPosition = bw.BaseStream.Position;

            // Write sections
            bw.BaseStream.Position += 16;
            for (var i = 0; i < Sections.Count; i++)
            {
                var section = Sections[i];

                // Update next section offset
                var nextSectionOffset = 0;
                if (i + 1 != Sections.Count)
                    nextSectionOffset = section.GetLength();
                section.Header.nextSectionOffset = nextSectionOffset;

                // Write section
                section.Write(bw);
            }

            // Write optional data
            if (Data != null)
                bw.Write(Data);

            var endPosition = bw.BaseStream.Position;

            // Write header
            Header.sectionSize = (int)(endPosition - startPosition);

            bw.BaseStream.Position = startPosition;
            WriteSectionHeader(Header, bw);

            // Skip section
            bw.BaseStream.Position = endPosition;
        }

        private static SectionHeader ReadSectionHeader(BinaryReaderX reader)
        {
            return new SectionHeader
            {
                magic = reader.ReadString(4),
                sectionSize = reader.ReadInt32(),
                zero0 = reader.ReadInt32(),
                nextSectionOffset = reader.ReadInt32()
            };
        }

        private static void WriteSectionHeader(SectionHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.sectionSize);
            writer.Write(header.zero0);
            writer.Write(header.nextSectionOffset);
        }
    }

    class Section
    {
        public SectionHeader Header { get; }

        public byte[] Data { get; }

        private Section(SectionHeader header, byte[] data)
        {
            Header = header;
            Data = data;
        }

        public static Section Read(BinaryReaderX br)
        {
            var header = ReadSectionHeader(br);
            var data = br.ReadBytes(header.sectionSize - 0x10);

            return new Section(header, data);
        }

        public int GetLength()
        {
            return 16 + Data.Length;
        }

        public void Write(BinaryWriterX bw)
        {
            var startPosition = bw.BaseStream.Position;

            // Write data
            bw.BaseStream.Position += 16;
            bw.Write(Data);

            var endPosition = bw.BaseStream.Position;

            // Write header
            Header.sectionSize = (int)(endPosition - startPosition);

            bw.BaseStream.Position = startPosition;
            WriteSectionHeader(Header, bw);

            // Skip section
            bw.BaseStream.Position = endPosition;
        }

        private static SectionHeader ReadSectionHeader(BinaryReaderX reader)
        {
            return new SectionHeader
            {
                magic = reader.ReadString(4),
                sectionSize = reader.ReadInt32(),
                zero0 = reader.ReadInt32(),
                nextSectionOffset = reader.ReadInt32()
            };
        }

        private static void WriteSectionHeader(SectionHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.sectionSize);
            writer.Write(header.zero0);
            writer.Write(header.nextSectionOffset);
        }
    }

    #endregion

    class AifSupport
    {
        public static EncodingDefinition GetEncodingDefinition()
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddColorEncoding(0x02, ImageFormats.L8());
            encodingDefinition.AddColorEncoding(0x06, ImageFormats.Rgba4444());
            encodingDefinition.AddColorEncoding(0x07, ImageFormats.La88());
            encodingDefinition.AddColorEncoding(0x08, ImageFormats.Rgba8888());
            encodingDefinition.AddColorEncoding(0x22, ImageFormats.La44());
            encodingDefinition.AddColorEncoding(0x24, ImageFormats.Rgb888());
            encodingDefinition.AddColorEncoding(0x25, ImageFormats.Etc1(true));
            encodingDefinition.AddColorEncoding(0x26, ImageFormats.Etc1A4(true));

            return encodingDefinition;
        }
    }
}
