using System.Diagnostics;
using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_cattle_call.Archives
{
    class Pak
    {
        private static readonly int EntrySize = 0x8;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read entries
            input.Position = header.entryOffset;
            var entries = ReadEntries(br, header.fileCount);

            // Read names
            input.Position = header.nameTable;
            var names = ReadStrings(br).ToArray();

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entry.size);
                var fileName = names[i];

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var nameOffset = 8;

            // Build string tree
            var fileNames = files.Select(x => x.FilePath.ToRelative().FullName).ToArray();

            var rootNode = new StringNode();
            rootNode.AddRange(fileNames);

            // Assign offsets to nodes
            var nodeOffsetMap = new Dictionary<StringNode, int>();
            var nameTableOffset = AssignOffsets(rootNode, nameOffset, nodeOffsetMap);
            nameTableOffset = (nameTableOffset + 1) & ~1;

            // Write node tree
            output.Position = nameTableOffset;
            var fileId = 0;
            WriteNodes(rootNode, bw, nodeOffsetMap, ref fileId);

            var entryOffset = bw.BaseStream.Length;
            var fileOffset = entryOffset + files.Count * EntrySize;

            // Write files
            var entries = new List<PakEntry>();

            var filePosition = fileOffset;
            foreach (var file in files)
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                entries.Add(new PakEntry
                {
                    offset = (int)filePosition,
                    size = (int)writtenSize
                });

                filePosition += writtenSize;
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write strings
            foreach (var pair in nodeOffsetMap)
            {
                output.Position = pair.Value;
                bw.WriteString(pair.Key.Text, Encoding.ASCII);
            }

            // Write header
            var header = new PakHeader
            {
                fileCount = (short)files.Count,
                entryOffset = (int)entryOffset,
                nameTable = (short)nameTableOffset
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private IEnumerable<string> ReadStrings(BinaryReaderX br, string currentName = "")
        {
            var nodeCount = br.ReadInt16();

            for (var i = 0; i < nodeCount; i++)
            {
                var stringOffset = br.ReadInt16();
                var flags = br.ReadInt16();

                var tablePosition = br.BaseStream.Position;

                if ((flags & 0x1) > 0)
                {
                    br.BaseStream.Position = stringOffset;
                    yield return currentName + br.ReadNullTerminatedString();
                }
                else
                {
                    br.BaseStream.Position = stringOffset;
                    var part = br.ReadNullTerminatedString();

                    br.BaseStream.Position = flags >> 1;
                    foreach (var name in ReadStrings(br, currentName + part))
                        yield return name;
                }

                br.BaseStream.Position = tablePosition;
            }
        }

        private int AssignOffsets(StringNode node, int offset, IDictionary<StringNode, int> nodeOffsetMap)
        {
            foreach (var internalNode in node.Nodes)
            {
                offset = AssignOffsets(internalNode, offset, nodeOffsetMap);

                if (!string.IsNullOrEmpty(node.Text))
                {
                    var sameNode = nodeOffsetMap.Keys.FirstOrDefault(x => x.Text == node.Text);
                    nodeOffsetMap[node] = sameNode != null ? nodeOffsetMap[sameNode] : offset;

                    if (sameNode == null)
                        offset += node.Text.Length + 1;
                }
            }

            if (!string.IsNullOrEmpty(node.Text))
            {
                var sameNode = nodeOffsetMap.Keys.FirstOrDefault(x => x.Text == node.Text);
                nodeOffsetMap[node] = sameNode != null ? nodeOffsetMap[sameNode] : offset;

                if (sameNode == null)
                    offset += node.Text.Length + 1;
            }

            return offset;
        }

        private void WriteNodes(StringNode node, BinaryWriterX bw, IDictionary<StringNode, int> nodeOffsetMap, ref int fileId)
        {
            bw.Write((short)node.Nodes.Count);

            var nextPosition = bw.BaseStream.Position + node.Nodes.Count * 4;
            foreach (var internalNode in node.Nodes)
            {
                var currentPosition = bw.BaseStream.Position;

                var isEnd = internalNode.Nodes.Count <= 0;
                var flags = isEnd ? (fileId++ << 1) | 1 : nextPosition << 1;

                if (internalNode.Nodes.Count > 0)
                {
                    bw.BaseStream.Position = nextPosition;
                    WriteNodes(internalNode, bw, nodeOffsetMap, ref fileId);

                    nextPosition = bw.BaseStream.Position;
                }

                bw.BaseStream.Position = currentPosition;
                bw.Write((short)nodeOffsetMap[internalNode]);
                bw.Write((short)flags);
            }

            bw.BaseStream.Position = nextPosition;
        }

        private PakHeader ReadHeader(BinaryReaderX reader)
        {
            return new PakHeader
            {
                fileCount = reader.ReadInt16(),
                entryOffset = reader.ReadInt32(),
                nameTable = reader.ReadInt16()
            };
        }

        private PakEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new PakEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private PakEntry ReadEntry(BinaryReaderX reader)
        {
            return new PakEntry
            {
                size = reader.ReadInt32(),
                offset = reader.ReadInt32()
            };
        }

        private void WriteHeader(PakHeader header, BinaryWriterX writer)
        {
            writer.Write(header.fileCount);
            writer.Write(header.entryOffset);
            writer.Write(header.nameTable);
        }

        private void WriteEntries(IList<PakEntry> entries, BinaryWriterX writer)
        {
            foreach (PakEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(PakEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.size);
            writer.Write(entry.offset);
        }
    }
}
