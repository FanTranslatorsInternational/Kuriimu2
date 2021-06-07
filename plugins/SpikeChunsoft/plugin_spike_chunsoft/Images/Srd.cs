using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_spike_chunsoft.Images
{
    class Srd
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(SrdHeader));

        private SrdSection _rsfSection;

        public IList<ImageInfo> Load(Stream srdStream, Stream srdvStream)
        {
            using var br = new BinaryReaderX(srdStream, ByteOrder.BigEndian);

            // Read sections
            var sections = new List<SrdSection>();
            while (srdStream.Position < srdStream.Length)
                sections.Add(br.ReadType<SrdSection>());

            _rsfSection = sections[1];

            // Add image infos
            var imageInfos = new List<ImageInfo>();
            foreach (var imageSection in sections.Skip(2))
            {
                if (imageSection.header.magic != "$TXR")
                    continue;

                // Read information
                var mipCount = imageSection.subData[0x13];
                var width = BinaryPrimitives.ReadInt16LittleEndian(imageSection.sectionData[6..]);
                var height = BinaryPrimitives.ReadInt16LittleEndian(imageSection.sectionData[8..]);
                var format = imageSection.sectionData[0xC];
                var hasSwizzle = (BinaryPrimitives.ReadInt16LittleEndian(imageSection.sectionData[4..]) & 1) == 0;

                // Read name
                var nameOffset = BinaryPrimitives.ReadInt32LittleEndian(imageSection.subData[0x1C..]);
                var name = Encoding.ASCII.GetString(imageSection.subData[(0x10 + nameOffset)..].TakeWhile(b => b != 0).ToArray());

                // Read image data
                var dataOffset = BinaryPrimitives.ReadInt32LittleEndian(imageSection.subData[0x20..]) & 0xFFFFFF;
                var dataLength = BinaryPrimitives.ReadInt32LittleEndian(imageSection.subData[0x24..]);
                var imgData = new byte[dataLength];

                srdvStream.Position = dataOffset;
                srdvStream.Read(imgData);

                var imageInfo = new SrdImageInfo(imgData, format, new Size(width, height), imageSection) { Name = name };
                if (hasSwizzle)
                    imageInfo.RemapPixels.With(context => new VitaSwizzle(context));
                else
                // TODO: Remove with pre-swizzle in encoding
                    if (SrdSupport.Formats[format].ColorsPerValue > 1)
                    imageInfo.RemapPixels.With(context => new BcSwizzle(context));

                // Read mips
                var mips = new List<byte[]>();
                for (var i = 1; i < mipCount; i++)
                {
                    width >>= 1;
                    height >>= 1;

                    dataOffset = BinaryPrimitives.ReadInt32LittleEndian(imageSection.subData[(0x20 + i * 0x10)..]) & 0xFFFFFF;
                    dataLength = BinaryPrimitives.ReadInt32LittleEndian(imageSection.subData[(0x24 + i * 0x10)..]);
                    var mipData = new byte[dataLength];

                    srdvStream.Position = dataOffset;
                    srdvStream.Read(mipData);

                    mips.Add(mipData);
                }

                imageInfo.MipMapData = mips;
                imageInfos.Add(imageInfo);
            }

            return imageInfos;
        }

        public void Save(Stream srdStream, Stream srdvStream, IList<ImageInfo> imageInfos)
        {
            using var bw = new BinaryWriterX(srdStream);

            // Calculate offsets
            var rsfOffset = HeaderSize;
            var texSectionOffset = (rsfOffset + HeaderSize + _rsfSection.header.sectionSize + 0xF) & ~0xF;

            // Write textures
            var texDataPosition = 0;

            var texSectionPosition = texSectionOffset;
            foreach (var imageInfo in imageInfos.Cast<SrdImageInfo>())
            {
                // Update and write section data
                BinaryPrimitives.TryWriteInt16LittleEndian(imageInfo.Section.sectionData[6..], (short)imageInfo.ImageSize.Width);
                BinaryPrimitives.TryWriteInt16LittleEndian(imageInfo.Section.sectionData[8..], (short)imageInfo.ImageSize.Height);
                imageInfo.Section.sectionData[0xC] = (byte)imageInfo.ImageFormat;

                srdStream.Position = texSectionPosition + HeaderSize;
                srdStream.Write(imageInfo.Section.sectionData);

                // Update and write sub data part 1
                imageInfo.Section.subData[0x13] = (byte)(imageInfo.MipMapCount + 1);
                BinaryPrimitives.WriteInt32LittleEndian(imageInfo.Section.subData[0x1C..], 0x10 + (imageInfo.MipMapCount + 1) * 0x10);

                srdStream.Write(imageInfo.Section.subData[..0x20]);

                // Write mip levels
                bw.Write(texDataPosition + 0x40000000);
                bw.Write(imageInfo.ImageData.Length);
                bw.Write(0x80);
                bw.Write(0);

                srdvStream.Write(imageInfo.ImageData);
                texDataPosition += (imageInfo.ImageData.Length + 0x7F) & ~0x7F;

                for (var i = 0; i < imageInfo.MipMapCount; i++)
                {
                    bw.Write(texDataPosition + 0x40000000);
                    bw.Write(imageInfo.MipMapData[i].Length);
                    bw.Write(0x80000000);
                    bw.Write(0);

                    srdvStream.Write(imageInfo.ImageData);
                    texDataPosition += (imageInfo.MipMapData[i].Length + 0x7F) & ~0x7F;
                }

                // Write sub data part 2
                bw.WriteString(imageInfo.Name, Encoding.ASCII, false);
                bw.WriteAlignment();

                bw.WriteType(new SrdHeader { magic = "$CT0" });

                // Update and write header information
                imageInfo.Section.header.subDataSize = (int)(srdStream.Position - texSectionPosition - HeaderSize - 0x10);

                var newTexSectionPosition = srdStream.Position;
                srdStream.Position = texSectionPosition;
                bw.WriteType(imageInfo.Section.header);

                texSectionPosition = (int)newTexSectionPosition;
            }

            srdStream.Position = srdStream.Length;
            bw.WriteType(new SrdHeader { magic = "$CT0" });

            // Write file start
            srdStream.Position = 0;
            bw.WriteType(new SrdHeader { magic = "$CFH", unk1 = 1 });

            bw.WriteType(_rsfSection);
        }
    }
}
