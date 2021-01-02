using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kryptography.Hash.Crc;

namespace plugin_level5.Switch.Archives
{
    // Game: Yo-kai Watch 4
    class G4pk
    {
        private readonly int _headerSize = Tools.MeasureType(typeof(G4pkHeader));
        private readonly int _offsetSize = Tools.MeasureType(typeof(int));
        private readonly int _lengthSize = Tools.MeasureType(typeof(int));
        private readonly int _hashSize = Tools.MeasureType(typeof(int));
        private readonly int _unkIdsSize = Tools.MeasureType(typeof(short));
        private readonly int _stringOffsetSize = Tools.MeasureType(typeof(short));

        private G4pkHeader _header;
        private IList<short> _unkIds;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Header
            _header = br.ReadType<G4pkHeader>();

            // Entry information
            br.BaseStream.Position = _header.headerSize;
            var fileOffsets = br.ReadMultiple<int>(_header.fileCount);
            var fileSizes = br.ReadMultiple<int>(_header.fileCount);
            var hashes = br.ReadMultiple<uint>(_header.table2EntryCount);

            // Unknown information
            _unkIds = br.ReadMultiple<short>(_header.table3EntryCount / 2);

            // Strings
            br.BaseStream.Position = (br.BaseStream.Position + 3) & ~3;
            var stringOffset = br.BaseStream.Position;
            var stringOffsets = br.ReadMultiple<short>(_header.table3EntryCount / 2);

            //Files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                br.BaseStream.Position = stringOffset + stringOffsets[i];
                var name = br.ReadCStringASCII();

                var fileStream = new SubStream(input, _header.headerSize + (fileOffsets[i] << 2), fileSizes[i]);
                result.Add(new ArchiveFileInfo(fileStream, name));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            var fileOffsetsPosition = _headerSize;
            var fileHashesPosition = fileOffsetsPosition + files.Count * (_offsetSize + _lengthSize);
            var unkIdsPosition = fileHashesPosition + files.Count * _hashSize;
            var stringOffsetPosition = (unkIdsPosition + _unkIdsSize + 3) & ~3;
            var stringPosition = (stringOffsetPosition + _stringOffsetSize + 3) & ~3;

            // Write strings
            var crc32 = Crc32.Default;

            bw.BaseStream.Position = stringOffsetPosition;
            var fileHashes = new List<uint>();
            var relativeStringOffset = stringPosition - stringOffsetPosition;
            for (var i = 0; i < files.Count; i++)
            {
                // Write string offset
                bw.BaseStream.Position = stringOffsetPosition + i * _stringOffsetSize;
                bw.Write((short)relativeStringOffset);

                // Add hash
                fileHashes.Add(BinaryPrimitives.ReadUInt32BigEndian(crc32.Compute(Encoding.ASCII.GetBytes(files[i].FilePath.ToRelative().FullName))));

                // Write string
                bw.BaseStream.Position = stringOffsetPosition + relativeStringOffset;
                bw.WriteString(files[i].FilePath.ToRelative().FullName, Encoding.ASCII, false);

                relativeStringOffset = (int)(bw.BaseStream.Position - stringOffsetPosition);
            }

            var fileDataPosition = (bw.BaseStream.Position + 3) & ~3;

            // Write file data
            bw.BaseStream.Position = fileDataPosition;
            var fileOffset = new List<int>();
            var fileSizes = new List<int>();
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                fileOffset.Add((int)((bw.BaseStream.Position - _headerSize) >> 2));

                var writtenSize = file.SaveFileData(bw.BaseStream, null);
                bw.WriteAlignment(0x20);

                fileSizes.Add((int)writtenSize);
            }

            // Write file information
            bw.BaseStream.Position = fileOffsetsPosition;
            bw.WriteMultiple(fileOffset);
            bw.WriteMultiple(fileSizes);
            bw.WriteMultiple(fileHashes);

            // Write unknown information
            bw.WriteMultiple(_unkIds);

            // Write header
            bw.BaseStream.Position = 0;

            _header.fileCount = files.Count;
            _header.contentSize = (int)(bw.BaseStream.Length - _headerSize);
            _header.table2EntryCount = (short)fileHashes.Count;

            bw.WriteType(_header);
        }
    }
}
