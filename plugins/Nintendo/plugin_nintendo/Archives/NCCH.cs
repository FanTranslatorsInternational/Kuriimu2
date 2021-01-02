using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_nintendo.Archives
{
    public class NCCH
    {
        private const int MediaSize_ = 0x200;

        private const string ExHeaderFileName_ = "ExHeader.bin";
        private const string PlainRegionFileName_ = "PlainRegion.bin";
        private const string LogoRegionFileName_ = "Logo.icn";
        private const string ExeFsFolder_ = "ExeFs";
        private const string RomFsFolder_ = "RomFs";

        private static int _ncchHeaderSize = Tools.MeasureType(typeof(NcchHeader));
        private static int _exeFsHeaderSize = Tools.MeasureType(typeof(NcchExeFsHeader));

        private NcchHeader _ncchHeader;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _ncchHeader = br.ReadType<NcchHeader>();

            var result = new List<IArchiveFileInfo>();

            // Add ExtendedHeader
            if (_ncchHeader.exHeaderSize != 0)
            {
                // ExHeader is stored 2 times, but stored size only reflects one of them
                var exHeaderStream = new SubStream(input, br.BaseStream.Position, _ncchHeader.exHeaderSize * 2);
                result.Add(new ArchiveFileInfo(exHeaderStream, ExHeaderFileName_));
            }

            // Add PlainRegion
            if (_ncchHeader.plainRegionOffset != 0 && _ncchHeader.plainRegionSize != 0)
            {
                var plainRegionStream = new SubStream(input, _ncchHeader.plainRegionOffset * MediaSize_, _ncchHeader.plainRegionSize * MediaSize_);
                result.Add(new ArchiveFileInfo(plainRegionStream, PlainRegionFileName_));
            }

            // Add LogoRegion
            if (_ncchHeader.logoRegionOffset != 0 && _ncchHeader.logoRegionSize != 0)
            {
                var logoStream = new SubStream(input, _ncchHeader.logoRegionOffset * MediaSize_, _ncchHeader.logoRegionSize * MediaSize_);
                result.Add(new ArchiveFileInfo(logoStream, LogoRegionFileName_));
                // TODO: Add Guid for logo icn
            }

            // Add ExeFS
            if (_ncchHeader.exeFsOffset != 0 && _ncchHeader.exeFsSize != 0)
            {
                // Read and resolve ExeFS data
                br.BaseStream.Position = _ncchHeader.exeFsOffset * MediaSize_;
                var exeFs = br.ReadType<NcchExeFsHeader>();
                var exeFsFilePosition = br.BaseStream.Position;

                // Add Files from ExeFS
                foreach (var file in exeFs.fileEntries)
                {
                    if (file.offset == 0 && file.size == 0)
                        break;

                    var exeFsFileStream = new SubStream(input, exeFsFilePosition + file.offset, file.size);
                    result.Add(new ArchiveFileInfo(exeFsFileStream, ExeFsFolder_ + "/" + file.name.TrimEnd('\0')));
                    // TODO: Add decompression if file.name == ".code" && (exHeader.sci.flag & 0x1) == 1
                }
            }

            // Add RomFS
            if (_ncchHeader.romFsOffset != 0 && _ncchHeader.romFsSize != 0)
            {
                // Read and resolve RomFS data
                br.BaseStream.Position = _ncchHeader.romFsOffset * MediaSize_;
                var romFs = new NcchRomFs(input);

                // Add Files from RomFS
                foreach (var file in romFs.Files)
                {
                    var romFsFileStream = new SubStream(br.BaseStream, file.fileOffset, file.fileSize);
                    result.Add(new ArchiveFileInfo(romFsFileStream, RomFsFolder_ + file.filePath));
                }
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var sha256 = new Kryptography.Hash.Sha256();
            using var bw = new BinaryWriterX(output);

            bw.BaseStream.Position = _ncchHeaderSize;

            // Write and update exHeader information
            var exHeaderFile = files.FirstOrDefault(f => f.FilePath.GetName() == ExHeaderFileName_);
            if (exHeaderFile != null)
            {
                var exHeaderPosition = bw.BaseStream.Position;
                var writtenSize = (exHeaderFile as ArchiveFileInfo).SaveFileData(output);

                bw.WriteAlignment(MediaSize_);

                _ncchHeader.exHeaderSize = (int)(exHeaderFile.FileSize / 2);
                _ncchHeader.exHeaderHash = sha256.Compute(new SubStream(output, exHeaderPosition, _ncchHeader.exHeaderSize));
            }
            else
            {
                Array.Clear(_ncchHeader.exHeaderHash, 0, 0x20);
                _ncchHeader.exHeaderSize = 0;
            }

            // Write and update logo region information
            var logoRegionFile = files.FirstOrDefault(f => f.FilePath.GetName() == LogoRegionFileName_);
            if (logoRegionFile != null)
            {
                var logoRegionPosition = bw.BaseStream.Position;
                var writtenSize = (logoRegionFile as ArchiveFileInfo).SaveFileData(output);

                bw.WriteAlignment(MediaSize_);

                _ncchHeader.logoRegionOffset = (int)(logoRegionPosition / MediaSize_);
                _ncchHeader.logoRegionSize = (int)((bw.BaseStream.Position - logoRegionPosition) / MediaSize_);
                _ncchHeader.logoRegionHash = sha256.Compute(new SubStream(output, logoRegionPosition, writtenSize));
            }
            else
            {
                _ncchHeader.logoRegionOffset = 0;
                _ncchHeader.logoRegionSize = 0;
                Array.Clear(_ncchHeader.logoRegionHash, 0, 0x20);
            }

            // Write and update plain region information
            var plainRegionFile = files.FirstOrDefault(f => f.FilePath.GetName() == PlainRegionFileName_);
            if (plainRegionFile != null)
            {
                var plainRegionPosition = bw.BaseStream.Position;
                (plainRegionFile as ArchiveFileInfo).SaveFileData(output);

                bw.WriteAlignment(MediaSize_);

                _ncchHeader.plainRegionOffset = (int)(plainRegionPosition / MediaSize_);
                _ncchHeader.plainRegionSize = (int)((bw.BaseStream.Position - plainRegionPosition) / MediaSize_);
            }
            else
            {
                _ncchHeader.plainRegionOffset = 0;
                _ncchHeader.plainRegionSize = 0;
            }

            // Write and update ExeFs
            var exeFsFiles = files.Where(x => x.FilePath.ToRelative().IsInDirectory(ExeFsFolder_, true)).ToArray();
            if (exeFsFiles.Any())
            {
                var exeFsPosition = bw.BaseStream.Position;
                var exeFsSize = ExeFsBuilder.Build(output, exeFsFiles);

                _ncchHeader.exeFsOffset = (int)(exeFsPosition / MediaSize_);
                _ncchHeader.exeFsSize = (int)(exeFsSize / MediaSize_);
                _ncchHeader.exeFsHashRegionSize = _exeFsHeaderSize / MediaSize_;
                _ncchHeader.exeFsSuperBlockHash = sha256.Compute(new SubStream(output, exeFsPosition, _exeFsHeaderSize));

                bw.WriteAlignment(0x1000);
            }
            else
            {
                _ncchHeader.exeFsOffset = 0;
                _ncchHeader.exeFsSize = 0;
                _ncchHeader.exeFsHashRegionSize = 0;
                Array.Clear(_ncchHeader.exeFsSuperBlockHash, 0, 0x20);
            }

            // Write and update RomFs
            var romFsFiles = files.Where(x => x.FilePath.ToRelative().IsInDirectory(RomFsFolder_, true)).ToArray();
            if (romFsFiles.Any())
            {
                var romFsPosition = bw.BaseStream.Position;
                var romFsSize1 = RomFsBuilder.CalculateRomFsSize(romFsFiles, RomFsFolder_);

                var buffer = new byte[0x4000];
                var size = romFsSize1;
                while (size > 0)
                {
                    var length = (int)Math.Min(size, 0x4000);
                    bw.BaseStream.Write(buffer, 0, length);

                    size -= length;
                }
                var romFsStream = new SubStream(bw.BaseStream, romFsPosition, romFsSize1);

                RomFsBuilder.Build(romFsStream, romFsFiles, RomFsFolder_);

                _ncchHeader.romFsOffset = (int)(romFsPosition / MediaSize_);
                _ncchHeader.romFsSize = (int)(romFsSize1 / MediaSize_);
                _ncchHeader.romFsHashRegionSize = 1;    // Only the first 0x200 of the RomFs get into the hash region apparently
                _ncchHeader.romFsSuperBlockHash = sha256.Compute(new SubStream(output, romFsPosition, MediaSize_));
            }
            else
            {
                _ncchHeader.romFsOffset = 0;
                _ncchHeader.romFsSize = 0;
                _ncchHeader.romFsHashRegionSize = 0;
                Array.Clear(_ncchHeader.romFsSuperBlockHash, 0, 0x20);
            }

            // Write header
            // HINT: Set NCCH flags to NoCrypto mode
            _ncchHeader.ncchFlags[7] = 4;
            _ncchHeader.ncchSize = (int)(output.Length / MediaSize_);

            bw.BaseStream.Position = 0;
            bw.WriteType(_ncchHeader);
        }
    }
}
