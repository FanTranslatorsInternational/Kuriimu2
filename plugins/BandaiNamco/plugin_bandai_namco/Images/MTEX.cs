using Kanvas.Contract.Enums.Swizzle;
using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_bandai_namco.Images
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MTEX
    {
        private MtexHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Ignore padding
            br.BaseStream.Position = 0x80;

            // Read texture
            var texture = br.ReadBytes((int)input.Length - 0x80);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = MtexSupport.GetEncodingDefinition().GetColorEncoding(_header.format)?.BitDepth ?? 0,
                ImageData = texture,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height),
                RemapPixels = context => new CtrSwizzle(context, CtrTransformation.YFlip),
                PadSize = builder => builder.ToPowerOfTwo()
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo image)
        {
            using var bw = new BinaryWriterX(output);

            // Header
            _header.width = (short)image.ImageSize.Width;
            _header.height = (short)image.ImageSize.Height;
            _header.format = (byte)image.ImageFormat;

            // Writing
            WriteHeader(_header, bw);

            bw.BaseStream.Position = 0x80;
            bw.Write(image.ImageData);
        }

        private MtexHeader ReadHeader(BinaryReaderX reader)
        {
            return new MtexHeader
            {
                magic = reader.ReadString(4),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadInt16(),
                width = reader.ReadInt16(),
                height = reader.ReadInt16(),
                unk4 = reader.ReadInt16(),
                format = reader.ReadInt16()
            };
        }

        private void WriteHeader(MtexHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.unk4);
            writer.Write(header.format);
        }
    }
}
