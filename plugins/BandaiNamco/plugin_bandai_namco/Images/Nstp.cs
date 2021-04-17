using System.Buffers.Binary;
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
    class Nstp
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(NstpHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(NstpImageHeader));

        private NstpHeader _header;

        public IList<ImageInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<NstpHeader>();

            // Read entries
            var entries = br.ReadMultiple<NstpImageHeader>(_header.imgCount);

            // Read image infos
            var result = new List<ImageInfo>();
            foreach (var entry in entries)
            {
                // Read name
                input.Position = entry.nameOffset;
                var name = br.ReadCStringASCII();

                // Read data
                input.Position = entry.dataOffset;
                var imgData = br.ReadBytes(entry.dataSize);

                var imageInfo = new NstpImageInfo(imgData, entry.format, new Size(entry.width, entry.height), entry)
                {
                    Name = name
                };
                imageInfo.RemapPixels.With(context => new NxSwizzle(context));

                result.Add(imageInfo);
            }

            return result;
        }

        public void Save(Stream output, IList<ImageInfo> imageInfos)
        {
            var crc32b = Crc32.Crc32B;
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var stringOffset = entryOffset + imageInfos.Count * EntrySize;
            var hashOffset = (stringOffset + imageInfos.Sum(x => x.Name.Length + 1) + 3) & ~3;
            var dataOffset = (hashOffset + imageInfos.Count * 8 + 0x7F) & ~0x7F;

            // Write image data
            var entries = new List<NstpImageHeader>();

            var stringPosition = stringOffset;
            var dataPosition = dataOffset;
            foreach (var imageInfo in imageInfos.Cast<NstpImageInfo>())
            {
                // Write data
                output.Position = dataPosition;
                bw.Write(imageInfo.ImageData);

                // Add entry
                imageInfo.Entry.dataOffset = dataPosition;
                imageInfo.Entry.dataSize = imageInfo.ImageData.Length;
                imageInfo.Entry.format = imageInfo.ImageFormat;
                imageInfo.Entry.width = (short)imageInfo.ImageSize.Width;
                imageInfo.Entry.height = (short)imageInfo.ImageSize.Height;
                imageInfo.Entry.nameOffset = stringPosition;
                entries.Add(imageInfo.Entry);

                dataPosition += (imageInfo.ImageData.Length + 0x3F) & ~0x3F;
                stringPosition += imageInfo.Name.Length + 1;
            }

            // Write hash entries
            output.Position = hashOffset;

            var hashEntries = imageInfos.Select((x, i) => (BinaryPrimitives.ReadUInt32BigEndian(crc32b.Compute(Encoding.ASCII.GetBytes(x.Name))), i));
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
