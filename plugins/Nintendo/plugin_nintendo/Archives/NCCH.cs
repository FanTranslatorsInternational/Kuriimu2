using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;
using Kryptography.Checksum;

namespace plugin_nintendo.Archives
{
    public class Ncch
    {
        private const int MediaSize_ = 0x200;

        private const string ExHeaderFileName_ = "ExHeader.bin";
        private const string PlainRegionFileName_ = "PlainRegion.bin";
        private const string LogoRegionFileName_ = "Logo.icn";
        private const string ExeFsFolder_ = "ExeFs";
        private const string RomFsFolder_ = "RomFs";

        private const int NcchHeaderSize_ = 0x200;
        private const int ExeFsHeaderSize_ = 0x200;

        private NcchHeader _ncchHeader;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _ncchHeader = ReadHeader(br);

            var result = new List<IArchiveFile>();

            // Add ExtendedHeader
            if (_ncchHeader.exHeaderSize != 0)
            {
                // ExHeader is stored 2 times, but stored size only reflects one of them
                var exHeaderStream = new SubStream(input, br.BaseStream.Position, _ncchHeader.exHeaderSize * 2);
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = ExHeaderFileName_,
                    FileData = exHeaderStream
                }));
            }

            // Add PlainRegion
            if (_ncchHeader.plainRegionOffset != 0 && _ncchHeader.plainRegionSize != 0)
            {
                var plainRegionStream = new SubStream(input, _ncchHeader.plainRegionOffset * MediaSize_, _ncchHeader.plainRegionSize * MediaSize_);
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = PlainRegionFileName_,
                    FileData = plainRegionStream
                }));
            }

            // Add LogoRegion
            if (_ncchHeader.logoRegionOffset != 0 && _ncchHeader.logoRegionSize != 0)
            {
                var logoStream = new SubStream(input, _ncchHeader.logoRegionOffset * MediaSize_, _ncchHeader.logoRegionSize * MediaSize_);
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = LogoRegionFileName_,
                    FileData = logoStream
                }));
                // TODO: Add Guid for logo icn
            }

            // Add ExeFS
            if (_ncchHeader.exeFsOffset != 0 && _ncchHeader.exeFsSize != 0)
            {
                // Read and resolve ExeFS data
                br.BaseStream.Position = _ncchHeader.exeFsOffset * MediaSize_;
                var exeFs = ReadExeFsHeader(br);
                var exeFsFilePosition = br.BaseStream.Position;

                // Add Files from ExeFS
                foreach (var file in exeFs.fileEntries)
                {
                    if (file.offset == 0 && file.size == 0)
                        break;

                    var exeFsFileStream = new SubStream(input, exeFsFilePosition + file.offset, file.size);
                    result.Add(new ArchiveFile(new ArchiveFileInfo
                    {
                        FilePath = ExeFsFolder_ + "/" + file.name.TrimEnd('\0'),
                        FileData = exeFsFileStream
                    }));
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
                    result.Add(new ArchiveFile(new ArchiveFileInfo
                    {
                        FilePath = RomFsFolder_ + file.filePath,
                        FileData = romFsFileStream
                    }));
                }
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var hash = new Sha256();

            using var bw = new BinaryWriterX(output);

            bw.BaseStream.Position = NcchHeaderSize_;

            // Write and update exHeader information
            var exHeaderFile = files.FirstOrDefault(f => f.FilePath.GetName() == ExHeaderFileName_);
            if (exHeaderFile != null)
            {
                var exHeaderPosition = bw.BaseStream.Position;
                _ = exHeaderFile.WriteFileData(output);

                bw.WriteAlignment(MediaSize_);

                _ncchHeader.exHeaderSize = (int)(exHeaderFile.FileSize / 2);
                _ncchHeader.exHeaderHash = hash.Compute(new SubStream(output, exHeaderPosition, _ncchHeader.exHeaderSize));
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
                var writtenSize = logoRegionFile.WriteFileData(output);

                bw.WriteAlignment(MediaSize_);

                _ncchHeader.logoRegionOffset = (int)(logoRegionPosition / MediaSize_);
                _ncchHeader.logoRegionSize = (int)((bw.BaseStream.Position - logoRegionPosition) / MediaSize_);
                _ncchHeader.logoRegionHash = hash.Compute(new SubStream(output, logoRegionPosition, writtenSize));
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
                plainRegionFile.WriteFileData(output);

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
                _ncchHeader.exeFsHashRegionSize = ExeFsHeaderSize_ / MediaSize_;
                _ncchHeader.exeFsSuperBlockHash = hash.Compute(new SubStream(output, exeFsPosition, ExeFsHeaderSize_));

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
                _ncchHeader.romFsSuperBlockHash = hash.Compute(new SubStream(output, romFsPosition, MediaSize_));
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
            WriteHeader(_ncchHeader, bw);
        }

        private NcchHeader ReadHeader(BinaryReaderX reader)
        {
            return new NcchHeader
            {
                rsa2048 = reader.ReadBytes(0x100),
                magic = reader.ReadString(4),
                ncchSize = reader.ReadInt32(),
                partitionId = reader.ReadUInt64(),
                makerCode = reader.ReadInt16(),
                version = reader.ReadInt16(),
                seedHashVerifier = reader.ReadUInt32(),
                programID = reader.ReadUInt64(),
                reserved1 = reader.ReadBytes(0x10),
                logoRegionHash = reader.ReadBytes(0x20),
                productCode = reader.ReadBytes(0x10),
                exHeaderHash = reader.ReadBytes(0x20),
                exHeaderSize = reader.ReadInt32(),
                reserved2 = reader.ReadInt32(),
                ncchFlags = reader.ReadBytes(0x8),
                plainRegionOffset = reader.ReadInt32(),
                plainRegionSize = reader.ReadInt32(),
                logoRegionOffset = reader.ReadInt32(),
                logoRegionSize = reader.ReadInt32(),
                exeFsOffset = reader.ReadInt32(),
                exeFsSize = reader.ReadInt32(),
                exeFsHashRegionSize = reader.ReadInt32(),
                reserved3 = reader.ReadInt32(),
                romFsOffset = reader.ReadInt32(),
                romFsSize = reader.ReadInt32(),
                romFsHashRegionSize = reader.ReadInt32(),
                reserved4 = reader.ReadInt32(),
                exeFsSuperBlockHash = reader.ReadBytes(0x20),
                romFsSuperBlockHash = reader.ReadBytes(0x20),
            };
        }

        private NcchExeFsHeader ReadExeFsHeader(BinaryReaderX reader)
        {
            return new NcchExeFsHeader
            {
                fileEntries = ReadExeFsEntries(reader, 0xA),
                reserved1 = reader.ReadBytes(0x20),
                fileEntryHashes = ReadExeFsEntryHashes(reader, 0xA)
            };
        }

        private NcchExeFsFileEntry[] ReadExeFsEntries(BinaryReaderX reader, int count)
        {
            var result = new NcchExeFsFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadExeFsEntry(reader);

            return result;
        }

        private NcchExeFsFileEntry ReadExeFsEntry(BinaryReaderX reader)
        {
            return new NcchExeFsFileEntry
            {
                name = reader.ReadString(8),
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private NcchExeFsFileEntryHash[] ReadExeFsEntryHashes(BinaryReaderX reader, int count)
        {
            var result = new NcchExeFsFileEntryHash[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadExeFsEntryHash(reader);

            return result;
        }

        private NcchExeFsFileEntryHash ReadExeFsEntryHash(BinaryReaderX reader)
        {
            return new NcchExeFsFileEntryHash
            {
                hash = reader.ReadBytes(0x20)
            };
        }

        private void WriteHeader(NcchHeader header, BinaryWriterX writer)
        {
            writer.Write(header.rsa2048);
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.ncchSize);
            writer.Write(header.partitionId);
            writer.Write(header.makerCode);
            writer.Write(header.version);
            writer.Write(header.seedHashVerifier);
            writer.Write(header.programID);
            writer.Write(header.reserved1);
            writer.Write(header.logoRegionHash);
            writer.Write(header.productCode);
            writer.Write(header.exHeaderHash);
            writer.Write(header.exHeaderSize);
            writer.Write(header.reserved2);
            writer.Write(header.ncchFlags);
            writer.Write(header.plainRegionOffset);
            writer.Write(header.plainRegionSize);
            writer.Write(header.logoRegionOffset);
            writer.Write(header.logoRegionSize);
            writer.Write(header.exeFsOffset);
            writer.Write(header.exeFsSize);
            writer.Write(header.exeFsHashRegionSize);
            writer.Write(header.reserved3);
            writer.Write(header.romFsOffset);
            writer.Write(header.romFsSize);
            writer.Write(header.romFsHashRegionSize);
            writer.Write(header.reserved4);
            writer.Write(header.exeFsSuperBlockHash);
            writer.Write(header.romFsSuperBlockHash);
        }
    }
}
