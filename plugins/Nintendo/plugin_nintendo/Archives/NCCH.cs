using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_nintendo.Archives
{
    public class NCCH
    {
        private const int MediaSize_ = 0x200;

        private static int _exeFsHeaderSize = Tools.MeasureType(typeof(NcchExeFsHeader));

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<NcchHeader>();

            var result = new List<ArchiveFileInfo>();

            // Add ExtendedHeader
            if (header.exHeaderSize != 0)
            {
                // ExHeader is stored 2 times, but stored size only reflects one of them
                var exHeaderStream = new SubStream(input, br.BaseStream.Position, header.exHeaderSize * 2);
                result.Add(new ArchiveFileInfo(exHeaderStream, "ExHeader.bin"));
            }

            // Add PlainRegion
            if (header.plainRegionOffset != 0 && header.plainRegionSize != 0)
            {
                var plainRegionStream = new SubStream(input, header.plainRegionOffset * MediaSize_, header.plainRegionSize * MediaSize_);
                result.Add(new ArchiveFileInfo(plainRegionStream, "PlainRegion.bin"));
            }

            // Add LogoRegion
            if (header.logoRegionOffset != 0 && header.logoRegionSize != 0)
            {
                var logoStream = new SubStream(input, header.logoRegionOffset * MediaSize_, header.logoRegionSize * MediaSize_);
                result.Add(new ArchiveFileInfo(logoStream, "Logo.icn"));
                // TODO: Add Guid for logo icn
            }

            // Add ExeFS
            if (header.exeFsOffset != 0 && header.exeFsSize != 0)
            {
                // Read and resolve ExeFS data
                br.BaseStream.Position = header.exeFsOffset * MediaSize_;
                var exeFs = br.ReadType<NcchExeFsHeader>();

                // Add Files from ExeFS
                foreach (var file in exeFs.fileHeaders)
                {
                    if (file.offset == 0 && file.size == 0)
                        break;

                    var exeFsFileStream = new SubStream(input, header.exeFsOffset * MediaSize_ + _exeFsHeaderSize + file.offset, file.size);
                    result.Add(new ArchiveFileInfo(exeFsFileStream, "ExeFs/" + file.name));
                    // TODO: Add decompression if file.name == ".code" && (exHeader.sci.flag & 0x1) == 1
                }
            }

            // Add RomFS
            if (header.romFSOffset != 0 && header.romFSSize != 0)
            {
                // Read and resolve RomFS data
                br.BaseStream.Position = header.romFSOffset * MediaSize_;
                var romFs = new NcchRomFs(input);

                // Add Files from RomFS
                foreach (var file in romFs.Files)
                {
                    var romFsFileStream = new SubStream(br.BaseStream, file.fileOffset, file.fileSize);
                    result.Add(new ArchiveFileInfo(romFsFileStream, "RomFS" + file.filePath));
                }
            }

            return result;
        }
    }
}
