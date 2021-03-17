using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.Image;
using Kryptography.Hash.Crc;
using plugin_level5.Switch.Images;

namespace plugin_level5.Switch.Archives
{
    // Hash: Crc32.Default
    class G4tx
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(G4txHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(G4txEntry));
        private static readonly int SubEntrySize = Tools.MeasureType(typeof(G4txSubEntry));

        private G4txHeader _header;
        private IList<G4txEntry> _entries;
        private IList<G4txSubEntry> _subEntries;
        private IList<byte> _ids;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<G4txHeader>();

            // Read entries
            _entries = br.ReadMultiple<G4txEntry>(_header.textureCount);
            _subEntries = br.ReadMultiple<G4txSubEntry>(_header.subTextureCount);
            br.SeekAlignment();

            // Skip hashes
            br.ReadMultiple<uint>(_header.totalCount);

            // Read ids
            _ids = br.ReadMultiple<byte>(_header.totalCount);
            br.SeekAlignment(4);

            // Prepare string reader
            var nxtchBase = (_header.headerSize + _header.tableSize + 0xF) & ~0xF;
            var stringSize = nxtchBase - input.Position;
            var stringStream = new SubStream(input, input.Position, stringSize);
            using var stringBr = new BinaryReaderX(stringStream);

            // Read string offsets
            var stringOffsets = br.ReadMultiple<short>(_header.totalCount);

            // Add files
            // TODO: Check if name is set by order of entries or ID
            var result = new List<IArchiveFileInfo>();
            var subEntryId = _header.textureCount;
            for (var i = 0; i < _header.textureCount; i++)
            {
                var entry = _entries[i];

                // Prepare base information
                stringStream.Position = stringOffsets[i];
                var name = stringBr.ReadCStringASCII();

                var fileStream = new SubStream(input, nxtchBase + entry.nxtchOffset, entry.nxtchSize);

                // Prepare sub entries
                var subEntries = new List<G4txSubTextureEntry>();
                foreach (var unkEntry in _subEntries.Where(x => x.entryId == i))
                {
                    stringStream.Position = stringOffsets[subEntryId];
                    var subName = stringBr.ReadCStringASCII();

                    subEntries.Add(new G4txSubTextureEntry(_ids[subEntryId++], unkEntry, subName));
                }

                result.Add(new G4txArchiveFileInfo(fileStream, name + ".nxtch", entry, _ids[i], subEntries)
                {
                    PluginIds = new[] { Guid.Parse("89222f8f-a345-45ed-9b79-e9e873bda1e9") }
                });
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var crc = Crc32.Default;
            var g4txFiles = files.Cast<G4txArchiveFileInfo>().ToArray();

            using var bw = new BinaryWriterX(output);
            using var br = new BinaryReaderX(output);

            // Calculate offsets
            var subEntryCount = g4txFiles.Sum(x => x.Entries.Count);

            var entryOffset = HeaderSize;
            var subEntryOffset = entryOffset + files.Count * EntrySize;
            var hashOffset = (subEntryOffset + subEntryCount * SubEntrySize + 0xF) & ~0xF;
            var idOffset = hashOffset + (files.Count + subEntryCount) * 4;
            var stringOffset = (idOffset + (files.Count + subEntryCount) + 0x3) & ~0x3;
            var stringContentOffset = (stringOffset + (files.Count + subEntryCount) * 2 + 0x7) & ~0x7;
            var dataOffset = (stringContentOffset + g4txFiles
                .Sum(x => x.FilePath.GetNameWithoutExtension().Length + 1 + x.Entries.Sum(y => y.Name.Length + 1)) + 0xF) & ~0xF;

            // Write files
            var dataPosition = dataOffset;
            foreach (var file in g4txFiles)
            {
                output.Position = dataPosition;
                var writtenSize = file.SaveFileData(output);

                // Update file entry
                output.Position = dataPosition;
                var nxtchHeader = br.ReadType<NxtchHeader>();

                file.Entry.nxtchOffset = dataPosition - dataOffset;
                file.Entry.nxtchSize = (int)(output.Length - dataPosition);
                file.Entry.width = (short)nxtchHeader.width;
                file.Entry.height = (short)nxtchHeader.height;

                dataPosition = (int)((dataPosition + writtenSize + 0xF) & ~0xF);
            }

            // Write strings
            var stringContentPosition = stringContentOffset;

            var names = files.Select(x => x.FilePath.GetNameWithoutExtension())
                .Concat(g4txFiles.SelectMany(x => x.Entries.Select(y => y.Name))).ToArray();
            var stringOffsets = new List<short>();
            foreach (var name in names)
            {
                stringOffsets.Add((short)(stringContentPosition - stringOffset));

                output.Position = stringContentPosition;
                bw.WriteString(name, Encoding.ASCII, false);

                stringContentPosition += name.Length + 1;
            }

            // Write string offsets
            output.Position = stringOffset;
            bw.WriteMultiple(stringOffsets);

            // Write ids
            var ids = g4txFiles.Select(x => x.Id).Concat(g4txFiles.SelectMany(x => x.Entries.Select(y => y.Id)));

            output.Position = idOffset;
            bw.WriteMultiple(ids);

            // Write hashes
            var hashes = names.Select(x => BinaryPrimitives.ReadUInt32BigEndian(crc.Compute(Encoding.ASCII.GetBytes(x))));

            output.Position = hashOffset;
            bw.WriteMultiple(hashes);

            // Write sub entries
            var subEntries = g4txFiles.SelectMany(x => x.Entries.Select(y => y.EntryEntry)).ToArray();

            output.Position = subEntryOffset;
            bw.WriteMultiple(subEntries);

            // Write entries
            var entries = g4txFiles.Select(x => x.Entry);

            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            _header.textureCount = (short)files.Count;
            _header.tableSize = stringContentPosition - HeaderSize;
            _header.textureDataSize = (int)output.Length - dataOffset;
            _header.subTextureCount = (byte)subEntries.Length;
            _header.totalCount = (short)(_header.textureCount + _header.subTextureCount);

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
