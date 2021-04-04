using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_bandai_namco.Images
{
    // TODO: index/palette data write

    /*https://docs.vitasdk.org/group__SceGxtUser.html*/
    /*https://github.com/xdanieldzd/Scarlet/blob/8d9e9cd34f6563da4a0f9b8797c3a1dd35542a4c/Scarlet/Platform/Sony/PSVita.cs*/
    public class Gxt
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(GxtHeader));
        private const int EntrySize_ = 0x20;

        private GxtFile _fileDesc;

        public IList<ImageInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Parse file description
            _fileDesc = br.ReadType<GxtFile>();

            // Create image infos
            var result = new List<ImageInfo>();
            foreach (var entry in _fileDesc.entries)
            {
                input.Position = entry.DataOffset;
                var imageData = br.ReadBytes(entry.DataSize);

                var imageInfo = new ImageInfo(imageData, entry.Format, new Size(entry.Width, entry.Height));
                imageInfo.RemapPixels.With(context => new VitaSwizzle(context));

                result.Add(imageInfo);
            }

            return result;
        }

        public void Save(Stream output, IList<ImageInfo> imageInfos)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var dataOffset = entryOffset + imageInfos.Count * EntrySize_;

            // Write image data
            var dataPosition = dataOffset;

            output.Position = dataOffset;
            for (var i = 0; i < imageInfos.Count; i++)
            {
                var entry = _fileDesc.entries[i];
                var imageInfo = imageInfos[i];

                // Update entry
                entry.DataOffset = dataPosition;
                entry.DataSize = imageInfo.ImageData.Length;
                entry.Format = imageInfo.ImageFormat;
                entry.Width = imageInfo.ImageSize.Width;
                entry.Height = imageInfo.ImageSize.Height;

                // Write image data
                bw.Write(imageInfo.ImageData);

                dataPosition += imageInfo.ImageData.Length;
            }

            // Write file description
            _fileDesc.header.dataOffset = dataOffset;
            _fileDesc.header.dataSize = (int)(output.Length - dataOffset);
            _fileDesc.header.texCount = imageInfos.Count;

            output.Position = 0;
            bw.WriteType(_fileDesc);
        }
    }
}
