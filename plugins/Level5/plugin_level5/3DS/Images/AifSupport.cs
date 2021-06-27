using System.Collections.Generic;
using System.Linq;
using Kanvas;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_level5._3DS.Images
{
    #region Section structs

    class SectionHeader
    {
        [FixedLength(4)]
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
            var header = br.ReadType<SectionHeader>();

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
            return AifSupport.SectionHeaderSize + Sections.Sum(x => x.GetLength()) + (Data?.Length ?? 0);
        }

        public Section GetSection(string magic)
        {
            return Sections.First(x => x.Header.magic == magic);
        }

        public void Write(BinaryWriterX bw)
        {
            var startPosition = bw.BaseStream.Position;

            // Write sections
            bw.BaseStream.Position += AifSupport.SectionHeaderSize;
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
            bw.WriteType(Header);

            // Skip section
            bw.BaseStream.Position = endPosition;
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
            var header = br.ReadType<SectionHeader>();
            var data = br.ReadBytes(header.sectionSize - 0x10);

            return new Section(header, data);
        }

        public int GetLength()
        {
            return AifSupport.SectionHeaderSize + Data.Length;
        }

        public void Write(BinaryWriterX bw)
        {
            var startPosition = bw.BaseStream.Position;

            // Write data
            bw.BaseStream.Position += AifSupport.SectionHeaderSize;
            bw.Write(Data);

            var endPosition = bw.BaseStream.Position;

            // Write header
            Header.sectionSize = (int)(endPosition - startPosition);

            bw.BaseStream.Position = startPosition;
            bw.WriteType(Header);

            // Skip section
            bw.BaseStream.Position = endPosition;
        }
    }

    #endregion

    class AifSupport
    {
        private static readonly IDictionary<int, IColorEncoding> CitraFormats = new Dictionary<int, IColorEncoding>
        {
            [0x02] = ImageFormats.L8(),

            [0x06] = ImageFormats.Rgba4444(),
            [0x07] = ImageFormats.La88(),
            [0x08] = ImageFormats.Rgba8888(),

            [0x22] = ImageFormats.La44(),

            [0x24] = ImageFormats.Rgb888(),
            [0x25] = ImageFormats.Etc1(true),
            [0x26] = ImageFormats.Etc1A4(true)
        };

        public static readonly int SectionHeaderSize = Tools.MeasureType(typeof(SectionHeader));

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(CitraFormats);

            return definition;
        }
    }
}
