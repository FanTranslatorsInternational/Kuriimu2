using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_sega.Images
{
    class Htex
    {
        private HtexHeader[] _headers;

        public ImageFileInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read 2 HTEX headers
            var header1 = ReadHeader(br);
            var header2 = ReadHeader(br);

            // Skip GBIX header
            var texHeader = ReadHeader(br);
            if (texHeader.magic == "GBIX")
            {
                _headers = new HtexHeader[4];
                _headers[2] = texHeader;

                texHeader = ReadHeader(br);
            }
            else
            {
                _headers = new HtexHeader[3];
            }

            _headers[0] = header1;
            _headers[1] = header2;
            _headers[^1] = texHeader;

            var format = texHeader.data1;
            var width = (int)(texHeader.data2 >> 16);
            var height = (int)(texHeader.data2 & 0xFFFF);

            var paletteData = br.ReadBytes(4 * 256);
            var imageData = br.ReadBytes(width * height);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = 8,
                ImageData = imageData,
                ImageFormat = 0,
                ImageSize = new Size(width, height),
                PaletteData = paletteData,
                PaletteFormat = (int)format,
                RemapPixels = context => new Ps2Swizzle(context)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            _headers[^1].data2 = (uint)(imageInfo.ImageSize.Width | (imageInfo.ImageSize.Height << 16));
            _headers[^1].data1 = (uint)imageInfo.PaletteFormat;

            foreach (var header in _headers)
                WriteHeader(header, bw);

            bw.Write(imageInfo.PaletteData);
            bw.Write(imageInfo.ImageData);

            WriteHeader(new HtexHeader { magic = "EOFC", data1 = 0x10 }, bw);
        }

        private HtexHeader ReadHeader(BinaryReaderX reader)
        {
            return new HtexHeader
            {
                magic = reader.ReadString(4),
                sectionSize = reader.ReadInt32(),
                data1 = reader.ReadUInt32(),
                data2 = reader.ReadUInt32()
            };
        }

        private void WriteHeader(HtexHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.sectionSize);
            writer.Write(header.data1);
            writer.Write(header.data2);
        }
    }
}
