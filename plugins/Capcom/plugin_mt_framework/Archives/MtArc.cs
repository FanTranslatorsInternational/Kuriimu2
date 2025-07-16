using System.Globalization;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_mt_framework.Archives
{
    class MtArc
    {
        private static readonly int HeaderSize = 0x8;

        private MtHeader _header;
        private MtArcPlatform _platform;

        public List<IArchiveFile> Load(Stream input, MtArcPlatform platform)
        {
            _platform = platform;

            switch (platform)
            {
                case MtArcPlatform.LittleEndian:
                    return LoadLittleEndian(input);

                case MtArcPlatform.BigEndian:
                    return LoadBigEndian(input);

                case MtArcPlatform.Switch:
                    return LoadSwitch(input);

                default:
                    throw new InvalidOperationException();
            }
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            switch (_platform)
            {
                case MtArcPlatform.LittleEndian:
                    SaveLittleEndian(output, files);
                    break;

                case MtArcPlatform.BigEndian:
                    SaveBigEndian(output, files);
                    break;

                case MtArcPlatform.Switch:
                    SaveSwitch(output, files);
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        public IArchiveFile Add(Stream fileData, UPath filePath)
        {
            // Determine extension hash
            var extension = filePath.GetExtensionWithDot()[1..];

            uint extensionHash;
            if (extension.Length != 8)
                extensionHash = MtArcSupport.DetermineExtensionHash(filePath.GetExtensionWithDot());
            else
            {
                if (!uint.TryParse(extension, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out extensionHash))
                    throw new InvalidOperationException($"Extension '{extension}' cannot be converted to a hash.");
            }

            // Create entry
            IMtEntry entry;
            switch (_platform)
            {
                case MtArcPlatform.Switch:
                    entry = new MtEntrySwitch
                    {
                        ExtensionHash = extensionHash,
                        FileName = (filePath.GetDirectory() / filePath.GetNameWithoutExtension()).FullName,
                        decompSize = (int)fileData.Length
                    };
                    break;

                case MtArcPlatform.LittleEndian:
                case MtArcPlatform.BigEndian:
                    entry = new MtEntry
                    {
                        ExtensionHash = extensionHash,
                        FileName = (filePath.GetDirectory() / filePath.GetNameWithoutExtension()).FullName,
                        decompSize = (int)fileData.Length,
                    };
                    break;

                default:
                    throw new InvalidOperationException();
            }

            // Create ArchiveFileInfo
            return CreateAfi(fileData, filePath.FullName, entry, _platform);
        }

        #region Load

        private List<IArchiveFile> LoadLittleEndian(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = MtArcSupport.ReadHeader(br);

            // Skip additional int under certain conditions
            if (_header.version != 7 && _header.version != 8)
                br.ReadInt32();

            // Determine if entries have extended file name section
            var firstEntry = MtArcSupport.ReadEntry(br);
            var hasExtendedName = firstEntry.extensionHash == 0 ||
                                  firstEntry.decompSize == 0 ||
                                  firstEntry.offset == 0;

            input.Position -= 0x50;

            // Read entries
            var entries = hasExtendedName ?
                MtArcSupport.ReadEntries<MtEntryExtendedName>(br, _header.entryCount) :
                MtArcSupport.ReadEntries<MtEntry>(br, _header.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.Offset, entry.CompSize);
                var fileName = entry.FileName + MtArcSupport.DetermineExtension(entry.ExtensionHash);

                result.Add(CreateAfi(fileStream, fileName, entry, _platform));
            }

            return result;
        }

        private List<IArchiveFile> LoadBigEndian(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            _header = MtArcSupport.ReadHeader(br);

            // Read entries
            var entries = MtArcSupport.ReadEntries<MtEntry>(br, _header.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.Offset, entry.CompSize);
                var fileName = entry.FileName + MtArcSupport.DetermineExtension(entry.ExtensionHash);

                result.Add(CreateAfi(fileStream, fileName, entry, _platform));
            }

            return result;
        }

        private List<IArchiveFile> LoadSwitch(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = MtArcSupport.ReadHeader(br);

            // Read entries
            var entries = MtArcSupport.ReadEntries<MtEntrySwitch>(br, _header.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.Offset, entry.CompSize);
                var fileName = entry.FileName + MtArcSupport.DetermineExtension(entry.ExtensionHash);

                // It seems every file is compressed with ZLib on Switch
                // Reasoning: Example file game.arc contains of at least one file "om120a" where compressed and uncompressed size are equal but the file is still compressed
                //            the decompressed file is really the same size; comparing with other entries no clear differences were found, that would indicate a
                //            compression flag
                result.Add(new MtArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = fileStream,
                    Compression = Compressions.ZLib.Build(),
                    DecompressedSize = entry.GetDecompressedSize(_platform)
                }, entry));
            }

            return result;
        }

        #endregion

        #region Save

        private void SaveLittleEndian(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            var isExtendedHeader = _header.version != 7 && _header.version != 8;

            // Calculate offsets
            var entryOffset = HeaderSize + (isExtendedHeader ? 4 : 0);
            var fileOffset = MtArcSupport.DetermineFileOffset(ByteOrder.LittleEndian, _header.version, files.Count, entryOffset, (files[0] as MtArchiveFile)?.Entry.GetType() == typeof(MtEntryExtendedName));

            // Write files
            var entries = new List<IMtEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<MtArchiveFile>())
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output, true);

                file.Entry.Offset = filePosition;
                file.Entry.SetDecompressedSize((int)file.FileSize, _platform);
                file.Entry.CompSize = (int)writtenSize;
                file.Entry.FileName = (file.FilePath.GetDirectory() / file.FilePath.GetNameWithoutExtension()).ToRelative().FullName;
                entries.Add(file.Entry);

                filePosition += (int)writtenSize;
            }

            // Write entries
            output.Position = entryOffset;
            MtArcSupport.WriteEntries(entries, bw);

            // Write header
            _header.entryCount = (short)files.Count;

            output.Position = 0;
            MtArcSupport.WriteHeader(_header, bw);
        }

        private void SaveBigEndian(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = MtArcSupport.DetermineFileOffset(ByteOrder.BigEndian, _header.version, files.Count, entryOffset, files[0].GetType() == typeof(MtEntryExtendedName));

            // Write files
            var entries = new List<IMtEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<MtArchiveFile>())
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output, true);

                file.Entry.Offset = filePosition;
                file.Entry.SetDecompressedSize((int)file.FileSize, _platform);
                file.Entry.CompSize = (int)writtenSize;
                file.Entry.FileName = (file.FilePath.GetDirectory() / file.FilePath.GetNameWithoutExtension()).ToRelative().FullName;
                entries.Add(file.Entry);

                filePosition += (int)writtenSize;
            }

            // Write entries
            output.Position = entryOffset;
            MtArcSupport.WriteEntries(entries, bw);

            // Write header
            _header.entryCount = (short)files.Count;

            output.Position = 0;
            MtArcSupport.WriteHeader(_header, bw);
        }

        private void SaveSwitch(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = MtArcSupport.DetermineFileOffset(ByteOrder.LittleEndian, _header.version, files.Count, entryOffset, false);

            // Write files
            var entries = new List<IMtEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<MtArchiveFile>())
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output, true);

                file.Entry.Offset = filePosition;
                file.Entry.SetDecompressedSize((int)file.FileSize, _platform);
                file.Entry.CompSize = (int)writtenSize;
                file.Entry.FileName = (file.FilePath.GetDirectory() / file.FilePath.GetNameWithoutExtension()).ToRelative().FullName;
                entries.Add(file.Entry);

                filePosition += (int)writtenSize;
            }

            // Write entries
            output.Position = entryOffset;
            MtArcSupport.WriteEntries(entries, bw);

            // Write header
            _header.entryCount = (short)files.Count;

            output.Position = 0;
            MtArcSupport.WriteHeader(_header, bw);
        }

        #endregion

        #region Support

        public static int GetArchiveSize(IList<IArchiveFile> files, int version, ByteOrder byteOrder)
        {
            // Get header size
            var isExtendedHeader = version != 7 && version != 8;
            var headerSize = HeaderSize + (isExtendedHeader ? 4 : 0);

            // Get file offset
            var fileOffset = MtArcSupport.DetermineFileOffset(byteOrder, version, files.Count, headerSize, files[0].GetType() == typeof(MtEntryExtendedName));

            // Add file sizes
            var fileRegionSize = (int)files.Cast<MtArchiveFile>().Sum(x => x.GetFinalStream().Length);

            return fileOffset + fileRegionSize;
        }

        public static IArchiveFile CreateAfi(Stream file, string fileName, IMtEntry entry, MtArcPlatform mtArcPlatform)
        {
            if (entry.CompSize == entry.GetDecompressedSize(mtArcPlatform))
                return new MtArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = file
                }, entry);

            var compMagic = file.ReadByte();
            if ((compMagic & 0xF) != 8 || (compMagic & 0xF0) > 0x70)
                throw new InvalidOperationException("File is marked as compressed but doesn't use ZLib.");

            return new MtArchiveFile(new CompressedArchiveFileInfo
            {
                FilePath = fileName,
                FileData = file,
                Compression = Compressions.ZLib.Build(),
                DecompressedSize = entry.GetDecompressedSize(mtArcPlatform)
            }, entry);

        }

        #endregion
    }
}
