using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_blue_reflection.Images
{
    class Kslt
    {
        private KsltHeader _header;
        private byte[][] _paddings;

        public List<ImageFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            _header = ReadHeader(br);

            br.BaseStream.Position = 0x40;

            _paddings = ReadUnknownPaddings(br, _header.FileCount);
            var offsets = ReadOffsets(br, _header.FileCount);

            var fileNames = new List<string>();
            for (int i = 0; i < _header.FileCount; i++)
                fileNames.Add(br.ReadNullTerminatedString());

            var result = new List<ImageFileInfo>();
            for (var i = 0; i < offsets.Length; i++)
            {
                var offset = offsets[i];
                br.BaseStream.Position = offset;

                var imgHeader = ReadImageHeader(br);
                var texture = br.ReadBytes(imgHeader.DataSize);

                result.Add(new KsltImageFileInfo(imgHeader)
                {
                    Name = fileNames[i],
                    BitDepth = KsltSupport.Formats[0].BitDepth,
                    ImageData = texture,
                    ImageFormat = 0,
                    ImageSize = new Size(imgHeader.Width, imgHeader.Height)
                });
            }

            return result;
        }

        public void Save(Stream output, IList<ImageFileInfo> imageInfos)
        {
            using var bw = new BinaryWriterX(output);

            WriteHeader(_header, bw);

            bw.BaseStream.Position = 0x40;

            WriteUnknownPaddings(_paddings, bw);

            var offsetTablePos = bw.BaseStream.Position;
            bw.BaseStream.Position += 0x14 * _header.FileCount;

            foreach (var imageInfo in imageInfos)
                bw.WriteString(imageInfo.Name, System.Text.Encoding.ASCII);

            var newOffsets = new List<int>();
            foreach (var imageInfo in imageInfos)
            {
                var kbi = imageInfo as KsltImageFileInfo;

                newOffsets.Add((int)bw.BaseStream.Position);

                kbi.Header.Width = (short)kbi.ImageSize.Width;
                kbi.Header.Height = (short)kbi.ImageSize.Height;
                kbi.Header.DataSize = kbi.ImageData.Length;

                WriteImageHeader(kbi.Header, bw);
                bw.Write(kbi.ImageData);
            }

            bw.BaseStream.Position = offsetTablePos;
            WriteOffsets(newOffsets, bw);
        }

        private KsltHeader ReadHeader(BinaryReaderX reader)
        {
            return new KsltHeader
            {
                Magic = reader.ReadString(8),
                FileCount = reader.ReadInt32(),
                FileSize = reader.ReadInt32(),
                OffsetTable = reader.ReadInt32(),
                FNameTableSize = reader.ReadInt32(),
                FileCount2 = reader.ReadInt32()
            };
        }

        private ImageHeader ReadImageHeader(BinaryReaderX reader)
        {
            return new ImageHeader
            {
                unk0 = reader.ReadInt32(),
                Width = reader.ReadInt16(),
                Height = reader.ReadInt16(),
                unk3 = reader.ReadInt32(),
                unk4 = reader.ReadInt32(),
                unk5 = reader.ReadInt32(),
                unk6 = reader.ReadInt32(),
                unk7 = reader.ReadInt32(),
                DataSize = reader.ReadInt32(),
                unk8 = reader.ReadInt32(),
                Padding = reader.ReadBytes(0x24)
            };
        }

        private byte[][] ReadUnknownPaddings(BinaryReaderX reader, int count)
        {
            var result = new byte[count][];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadBytes(0x38);

            return result;
        }

        private int[] ReadOffsets(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = reader.ReadInt32();
                reader.BaseStream.Position += 0x10;
            }

            return result;
        }

        private void WriteHeader(KsltHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.Magic, writeNullTerminator: false);
            writer.Write(header.FileCount);
            writer.Write(header.FileSize);
            writer.Write(header.OffsetTable);
            writer.Write(header.FNameTableSize);
            writer.Write(header.FileCount2);
        }

        private void WriteImageHeader(ImageHeader header, BinaryWriterX writer)
        {
            writer.Write(header.unk0);
            writer.Write(header.Width);
            writer.Write(header.Height);
            writer.Write(header.unk3);
            writer.Write(header.unk4);
            writer.Write(header.unk5);
            writer.Write(header.unk6);
            writer.Write(header.unk7);
            writer.Write(header.DataSize);
            writer.Write(header.unk8);
            writer.Write(header.Padding);
        }

        private void WriteUnknownPaddings(byte[][] paddings, BinaryWriterX writer)
        {
            foreach (byte[] padding in paddings)
                writer.Write(padding);
        }

        private void WriteOffsets(IList<int> offsets, BinaryWriterX writer)
        {
            foreach (int offset in offsets)
            {
                writer.Write(offset);
                writer.WritePadding(0x10);
            }
        }
    }
}
