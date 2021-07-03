using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace most_wanted_ent.Images
{
    class Ctgd
    {
        private IList<CtgdSection> _sections;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            var width = br.ReadUInt16();
            var height = br.ReadUInt16();

            // Read sections
            _sections = new List<CtgdSection>();
            while (input.Position < input.Length)
                _sections.Add(br.ReadType<CtgdSection>());

            // Get format
            var formatSection = _sections.FirstOrDefault(x => x.magic == "nns_frmt");
            var format = Encoding.ASCII.GetString(formatSection.data);

            // Get texel data
            var texelSection = _sections.FirstOrDefault(x => x.magic == "nns_txel");

            // Get palette data
            var paletteSection = _sections.FirstOrDefault(x => x.magic == "nns_pcol");

            // Create image info
            ImageInfo imageInfo;
            switch (format)
            {
                case "palette256":
                    imageInfo = new ImageInfo(texelSection.data, 0, new Size(width, height));

                    imageInfo.PaletteFormat = 0;
                    imageInfo.PaletteData = paletteSection.data;

                    break;

                default:
                    imageInfo = null;
                    break;
            }

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
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
            bw.WriteMultiple(_sections);
        }
    }
}
