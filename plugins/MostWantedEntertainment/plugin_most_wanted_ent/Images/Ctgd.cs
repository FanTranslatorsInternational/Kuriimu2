using System.Text;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_most_wanted_ent.Images
{
    class Ctgd
    {
        private IList<CtgdSection> _sections;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            var width = br.ReadUInt16();
            var height = br.ReadUInt16();

            // Read sections
            _sections = new List<CtgdSection>();
            while (input.Position < input.Length)
                _sections.Add(ReadSection(br));

            // Get format
            var formatSection = _sections.FirstOrDefault(x => x.magic == "nns_frmt");
            var format = Encoding.ASCII.GetString(formatSection.data);

            // Get texel data
            var texelSection = _sections.FirstOrDefault(x => x.magic == "nns_txel");

            // Get palette data
            var paletteSection = _sections.FirstOrDefault(x => x.magic == "nns_pcol");

            // Create image info
            ImageFileInfo imageInfo;
            switch (format)
            {
                case "palette256":
                    imageInfo = new ImageFileInfo
                    {
                        BitDepth = 8,
                        ImageData = texelSection.data,
                        ImageFormat = 0,
                        ImageSize = new Size(width, height),
                        PaletteData = paletteSection.data,
                        PaletteFormat = 0
                    };
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported image format {format}.");
            }

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output, true);

            // Update sections
            var texelSection = _sections.FirstOrDefault(x => x.magic == "nns_txel");
            texelSection.size = imageInfo.ImageData.Length + 0xC;
            texelSection.data = imageInfo.ImageData;

            var paletteSection = _sections.FirstOrDefault(x => x.magic == "nns_pcol");
            paletteSection.size = imageInfo.PaletteData.Length + 0xC;
            paletteSection.data = imageInfo.PaletteData;

            // Write dimensions
            bw.Write((ushort)imageInfo.ImageSize.Width);
            bw.Write((ushort)imageInfo.ImageSize.Height);

            // Write sections
            WriteSections(_sections, bw);
        }

        private CtgdSection ReadSection(BinaryReaderX reader)
        {
            var section = new CtgdSection
            {
                magic = reader.ReadString(8),
                size = reader.ReadInt32()
            };

            section.data = reader.ReadBytes(section.size - 0xC);

            return section;
        }

        private void WriteSections(IList<CtgdSection> sections, BinaryWriterX writer)
        {
            foreach (CtgdSection section in sections)
                WriteSection(section, writer);
        }

        private void WriteSection(CtgdSection section, BinaryWriterX writer)
        {
            writer.WriteString(section.magic, writeNullTerminator: false);
            writer.Write(section.size);
            writer.Write(section.data);
        }
    }
}
