using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_metal_max.Archives
{
    class Pak
    {
        private static readonly int EntrySize = Tools.MeasureType(typeof(PakEntry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<PakHeader>();

            // Read entries
            input.Position = header.entryOffset;
            var entries = br.ReadMultiple<PakEntry>(header.fileCount);

            // Read names
            input.Position = header.nameTable;
            var names = ReadStrings(br).ToArray();

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entry.size);
                var fileName = names[i];

                result.Add(new ArchiveFileInfo(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
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
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                entries.Add(new PakEntry
                {
                    offset = (int)filePosition,
                    size = (int)writtenSize
                });

                filePosition += writtenSize;
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write strings
            foreach (var pair in nodeOffsetMap)
            {
                output.Position = pair.Value;
                bw.WriteString(pair.Key.Text, Encoding.ASCII, false);
            }

            // Write header
            output.Position = 0;
            bw.WriteType(new PakHeader
            {
                fileCount = (short)files.Count,
                entryOffset = (int)entryOffset,
                nameTable = (short)nameTableOffset
            });
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
                    yield return currentName + br.ReadCStringASCII();
                }
                else
                {
                    br.BaseStream.Position = stringOffset;
                    var part = br.ReadCStringASCII();

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

                if (currentPosition == 0x18C)
                    Debugger.Break();

                bw.BaseStream.Position = currentPosition;
                bw.Write((short)nodeOffsetMap[internalNode]);
                bw.Write((short)flags);
            }

            bw.BaseStream.Position = nextPosition;
        }
    }
}
