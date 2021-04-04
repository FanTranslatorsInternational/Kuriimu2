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
        private const int P8PaletteSize_ = 256 * 4;
        private const int P4PaletteSize_ = 16 * 4;

        private GxtFile _fileDesc;

        public IList<ImageInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Parse file description
            _fileDesc = br.ReadType<GxtFile>();

            var p8PaletteOffset = _fileDesc.header.dataOffset + _fileDesc.header.dataSize -
                                  _fileDesc.header.p8PalCount * P8PaletteSize_;
            var p4PaletteOffset = p8PaletteOffset - _fileDesc.header.p4PalCount * P4PaletteSize_;

            // Create image infos
            var result = new List<ImageInfo>();
            foreach (var entry in _fileDesc.entries)
            {
                input.Position = entry.DataOffset;
                var imageData = br.ReadBytes(entry.DataSize);

                var imageInfo = new ImageInfo(imageData, entry.Format, new Size(entry.Width, entry.Height));

                // Apply correct swizzle
                switch ((uint)entry.Type)
                {
                    case 0x60000000:    // Linear
                        break;

                    case 0x00000000:    // Vita swizzle
                    case 0x40000000:
                        imageInfo.RemapPixels.With(context => new VitaSwizzle(context));
                        break;

                    case 0x80000000:
                        imageInfo.RemapPixels.With(context => new CtrSwizzle(context));
                        break;
                }

                // Add palette data if necessary
                if ((uint)entry.Format == 0x95000000)   // I8 palette
                {
                    input.Position = p8PaletteOffset + entry.PaletteIndex * P8PaletteSize_;

                    imageInfo.PaletteData = br.ReadBytes(P8PaletteSize_);
                    imageInfo.PaletteFormat = entry.SubFormat;
                }

                if ((uint)entry.Format == 0x94000000)   // I4 palette
                {
                    input.Position = p4PaletteOffset + entry.PaletteIndex * P4PaletteSize_;

                    imageInfo.PaletteData = br.ReadBytes(P4PaletteSize_);
                    imageInfo.PaletteFormat = entry.SubFormat;
                }

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
            var p4Index = 0;
            var p8Index = 0;

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
                entry.PaletteIndex = -1;

                if ((uint)imageInfo.ImageFormat == 0x94000000 || (uint)imageInfo.ImageFormat == 0x95000000)
                    entry.SubFormat = imageInfo.PaletteFormat;

                if ((uint) imageInfo.ImageFormat == 0x94000000)
                    entry.PaletteIndex = p4Index++;
                if ((uint)imageInfo.ImageFormat == 0x95000000)
                    entry.PaletteIndex = p8Index++;

                // Write image data
                bw.Write(imageInfo.ImageData);

                dataPosition += imageInfo.ImageData.Length;
            }

            // Write palette data
            foreach(var imageInfo in imageInfos)
            {
                if ((uint) imageInfo.ImageFormat == 0x94000000)
                {
                    bw.Write(imageInfo.PaletteData);
                    bw.WritePadding(P4PaletteSize_-imageInfo.PaletteData.Length);
                }
            }

            foreach (var imageInfo in imageInfos)
            {
                if ((uint) imageInfo.ImageFormat == 0x95000000)
                {
                    bw.Write(imageInfo.PaletteData);
                    bw.WritePadding(P8PaletteSize_ - imageInfo.PaletteData.Length);
                }
            }

            // Write file description
            _fileDesc.header.dataOffset = dataOffset;
            _fileDesc.header.dataSize = (int)(output.Length - dataOffset);
            _fileDesc.header.texCount = imageInfos.Count;
            _fileDesc.header.p4PalCount = p4Index;
            _fileDesc.header.p8PalCount = p8Index;

            output.Position = 0;
            bw.WriteType(_fileDesc);
        }
    }
}
