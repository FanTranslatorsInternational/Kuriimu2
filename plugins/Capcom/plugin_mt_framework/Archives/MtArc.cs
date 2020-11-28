using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_mt_framework.Archives
{
    class MtArc
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(MtHeader));

        private MtHeader _header;
        private ByteOrder _byteOrder;
        private Platform _platform;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Determine byte order
            var magic = br.ReadString(4);
            br.ByteOrder = _byteOrder = magic == "\0CRA" || magic == "\0SFH" ? ByteOrder.BigEndian : ByteOrder.LittleEndian;
            input.Position -= 4;

            // Header
            _header = br.ReadType<MtHeader>();

            // Determine possible platform the arc was found on
            _platform = MtArcSupport.DeterminePlatform(_byteOrder, _header);

            // Skip additional int under certain conditions
            if (_byteOrder == ByteOrder.LittleEndian && _header.version != 7 && _header.version != 8 && _header.version != 9)
                br.ReadInt32();

            // Read entries
            var entries = br.ReadMultiple(_header.entryCount, _header.version == 9 ? typeof(MtEntrySwitch) : typeof(MtEntry)).Cast<IMtEntry>();

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var subStream = new SubStream(input, entry.Offset, entry.CompSize);
                var fileName = entry.FileName.TrimEnd('\0') + MtArcSupport.DetermineExtension(entry.ExtensionHash);

                result.Add(CreateAfi(subStream, fileName, entry, _platform));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output, _byteOrder);

            var isExtendedHeader = _byteOrder == ByteOrder.LittleEndian && _header.version != 7 && _header.version != 8 && _header.version != 9;

            // Calculate offsets
            var entryOffset = HeaderSize + (isExtendedHeader ? 4 : 0);
            var fileOffset = MtArcSupport.DetermineFileOffset(_byteOrder, _header.version, files.Count, entryOffset);

            // Write files
            var entries = new List<IMtEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<MtArchiveFileInfo>())
            {
                output.Position = filePosition;

                var writtenSize = (int)file.SaveFileData(output);

                file.Entry.Offset = filePosition;
                file.Entry.SetDecompressedSize((int)file.FileSize, _platform);
                file.Entry.CompSize = writtenSize;
                entries.Add(file.Entry);

                filePosition += writtenSize;
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            _header.entryCount = (short)files.Count;

            output.Position = 0;
            bw.WriteType(_header);
        }

        private IArchiveFileInfo CreateAfi(Stream file, string fileName, IMtEntry entry, Platform platform)
        {
            // It seems every file is compressed with ZLib on Switch
            // Example file game.arc contains of at least one file "om120a" where compressed and uncompressed size are equal but the file is still compressed
            // the decompressed file is really the same size; comparing with other entries no clear differences were found, that would indicate a
            // compression flag
            if (platform == Platform.SWITCH)
                return new MtArchiveFileInfo(file, fileName, entry, Kompression.Implementations.Compressions.ZLib, entry.GetDecompressedSize(platform));

            if (entry.CompSize != entry.GetDecompressedSize(platform))
            {
                if (file.ReadByte() != 0x78)
                    throw new InvalidOperationException("File is marked as compressed but doesn't use ZLib.");

                return new MtArchiveFileInfo(file, fileName, entry, Kompression.Implementations.Compressions.ZLib, entry.GetDecompressedSize(platform));
            }

            return new MtArchiveFileInfo(file, fileName, entry);
        }
    }
}
