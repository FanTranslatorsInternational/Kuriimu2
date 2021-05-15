using System.Drawing;
using System.IO;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_felistella.Images
{
    class Tex
    {
        private TexHeader _header;
        private TexEntry _entry;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<TexHeader>();

            // Read entry
            input.Position = _header.entryOffset;
            _entry = br.ReadType<TexEntry>();

            // Create image info
            input.Position = _entry.dataOffset;
            var imgData = br.ReadBytes(_entry.dataSize);

            return new ImageInfo(imgData, 0, new Size(_entry.width, _entry.height));
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw=new BinaryWriterX(output);

            // Write image data
            output.Position = _entry.dataOffset;
            bw.Write(imageInfo.ImageData);
            bw.WritePadding(0x10);

            // Write entry
            _entry.width = (short)imageInfo.ImageSize.Width;
            _entry.height = (short)imageInfo.ImageSize.Height;
            _entry.dataSize = imageInfo.ImageData.Length;

            output.Position = _header.entryOffset;
            bw.WriteType(_entry);

            bw.Write(1);

            // Write header
            _header.dataSize = imageInfo.ImageData.Length + 0x20;
            _header.fileSize = (int)output.Length;

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
