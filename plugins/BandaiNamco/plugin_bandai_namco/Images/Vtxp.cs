using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;
using Kryptography.Hash.Crc;

namespace plugin_bandai_namco.Images
{
    class Vtxp
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(VtxpHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(VtxpImageEntry));

        private VtxpHeader _header;

        public IList<ImageInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<VtxpHeader>();

            // Read entries
            var entries = br.ReadMultiple<VtxpImageEntry>(_header.imgCount);

            // Read image infos
            var result = new List<ImageInfo>();
            foreach (var entry in entries)
            {
                // Read name
                input.Position = entry.nameOffset;
                var name = br.ReadCStringASCII();

                // Read palette
                input.Position = entry.paletteOffset;
                var paletteData = br.ReadBytes(entry.dataOffset - entry.paletteOffset);

                // Read data
                input.Position = entry.dataOffset;
                var imgData = br.ReadBytes(entry.dataSize);

                var format = entry.format >> 24 == 0x94 || entry.format >> 24 == 0x95 ? entry.format & 0xFFFF0000 : entry.format;
                var imageInfo = new VtxpImageInfo(imgData, (int)format, new Size(entry.width, entry.height), entry)
                {
                    Name = name
                };

                switch (entry.type)
                {
                    case 0x02:
                        imageInfo.RemapPixels.With(context => new VitaSwizzle(context));
                        break;
                }

                if ((uint)imageInfo.ImageFormat == 0x94000000 || (uint)imageInfo.ImageFormat == 0x95000000)
                {
                    imageInfo.PaletteData = paletteData;
                    imageInfo.PaletteFormat = (int)(entry.format & 0xFFFF);
                }

                result.Add(imageInfo);
            }

            return result;
        }

        public void Save(Stream output, IList<ImageInfo> imageInfos)
        {
            var crc32 = Crc32.Crc32B;
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var stringOffset = entryOffset + imageInfos.Count * EntrySize;
            var hashOffset = (stringOffset + imageInfos.Sum(x => x.Name.Length + 1) + 3) & ~3;
            var dataOffset = (hashOffset + imageInfos.Count * 8 + 0x7F) & ~0x7F;

            // Write image data
            var entries = new List<VtxpImageEntry>();

            var stringPosition = stringOffset;
            var dataPosition = dataOffset;
            foreach (var imageInfo in imageInfos.Cast<VtxpImageInfo>())
            {
                output.Position = dataPosition;

                // Write palette
                if (imageInfo.HasPaletteInformation)
                    bw.Write(imageInfo.PaletteData);

                // Write data
                bw.Write(imageInfo.ImageData);

                // Add entry
                imageInfo.Entry.paletteOffset = imageInfo.HasPaletteInformation ? dataPosition : 0;
                imageInfo.Entry.dataOffset = imageInfo.HasPaletteInformation ? dataPosition + imageInfo.PaletteData.Length : dataPosition;
                imageInfo.Entry.dataSize = imageInfo.ImageData.Length;
                imageInfo.Entry.width = (short)imageInfo.ImageSize.Width;
                imageInfo.Entry.height = (short)imageInfo.ImageSize.Height;
                imageInfo.Entry.nameOffset = stringPosition;

                imageInfo.Entry.format = (uint)imageInfo.ImageFormat;
                if ((uint)imageInfo.ImageFormat == 0x94000000 || (uint)imageInfo.ImageFormat == 0x95000000)
                    imageInfo.Entry.format |= (uint)imageInfo.PaletteFormat;

                entries.Add(imageInfo.Entry);

                // Increase positions
                stringPosition += imageInfo.Name.Length + 1;
                dataPosition += imageInfo.ImageData.Length + (imageInfo.HasPaletteInformation ? imageInfo.PaletteData.Length : 0);
                dataPosition = (dataPosition + 0x3F) & ~0x3F;
            }

            // Write hash entries
            output.Position = hashOffset;

            var hashEntries = imageInfos.Select((x, i) => (crc32.ComputeValue(x.Name), i));
            foreach (var (hash, index) in hashEntries.OrderBy(x => x.Item1))
            {
                bw.Write(hash);
                bw.Write(index);
            }

            // Write strings
            output.Position = stringOffset;
            foreach (var name in imageInfos.Select(x => x.Name))
                bw.WriteString(name, Encoding.ASCII, false);

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            _header.hashOffset = hashOffset;
            _header.imgCount = imageInfos.Count;

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
