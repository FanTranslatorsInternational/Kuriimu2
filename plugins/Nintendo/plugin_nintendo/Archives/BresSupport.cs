//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using Komponent.IO;
//using Komponent.IO.Attributes;
//using Kontract.Models.IO;

//namespace plugin_nintendo.Archives
//{
//    class BresFile
//    {
//        private ByteOrder _byteOrder;
//        private BresHeader _header;
//        private BresTree _tree;

//        public BresFile(Stream input)
//        {
//            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

//            // Determine byte order
//            input.Position = 4;
//            _byteOrder = br.ByteOrder = br.ReadType<ByteOrder>();

//            // Read bres header
//            input.Position = 0;
//            _header = br.ReadType<BresHeader>();

//            // Read tree structure
//            input.Position = _header.rootOffset;
//            _tree = new BresTree(br);
//        }

//        class BresTree
//        {
//            private BresGroupIndex _root;

//            public IList<BresFileEntry> FileEntries { get; }

//            public BresTree(BinaryReaderX br)
//            {
//                var rootOffset = br.BaseStream.Position;

//                // Read root section header
//                var header = br.ReadType<BresSectionHeader>();
//                var endOffset = rootOffset + header.sectionSize;

//                // Read root node
//                _root = ReadGroup(br, endOffset);

//                // Determine string offset
//                var stringOffset = CollectNodes(_root).Min(x => x.nameOffset) - 4;

//                // Read file entries
//                FileEntries = CollectFiles(br.BaseStream, _root, UPath.Root, stringOffset).ToArray();
//            }

//            private BresGroupIndex ReadGroup(BinaryReaderX br, long sectionEndOffset)
//            {
//                // Read group
//                var groupOffset = br.BaseStream.Position;
//                var group = br.ReadType<BresGroupIndex>();

//                // Read nested groups
//                foreach (var node in group.nodes)
//                {
//                    node.dataOffset += (int)groupOffset;
//                    node.nameOffset += (int)groupOffset;

//                    // Read named node
//                    br.BaseStream.Position = node.dataOffset;
//                    if (br.BaseStream.Position < sectionEndOffset)
//                        group.Groups.Add(ReadGroup(br, sectionEndOffset));
//                }

//                return group;
//            }

//            private IEnumerable<BresNode> CollectNodes(BresGroupIndex group)
//            {
//                foreach (var node in group.nodes)
//                {
//                    yield return node;

//                    foreach (var nestedNode in group.Groups.SelectMany(CollectNodes))
//                        yield return nestedNode;
//                }
//            }

//            private IEnumerable<BresFileEntry> CollectFiles(Stream input, BresGroupIndex group, UPath path, int dataEnd)
//            {

//            }

//            public class BresFileEntry
//            {
//                public BresNode Node { get; set; }

//                public Stream Data { get; set; }
//            }
//        }

//        #region Structures

//        class BresHeader
//        {
//            [FixedLength(4)]
//            public string magic = "bres";
//            [Endianness(ByteOrder = ByteOrder.BigEndian)]
//            public ByteOrder byteOrder;

//            public ushort zero0;
//            public int fileSize;
//            public short rootOffset;
//            public short sectionCount;
//        }

//        class BresSectionHeader
//        {
//            [FixedLength(4)]
//            public string magic = "root";
//            public int sectionSize;
//        }

//        class BresGroupIndex
//        {
//            public int groupSize;
//            public int nodeCount;

//            public BresNode rootNode;

//            [VariableLength("nodeCount")]
//            public BresNode[] nodes;

//            public IList<BresGroupIndex> Groups { get; } = new List<BresGroupIndex>();
//        }

//        class BresNode
//        {
//            public ushort id;
//            public ushort unk1;
//            public ushort leftIndex;
//            public ushort rightIndex;
//            public int nameOffset;
//            public int dataOffset;
//        }

//        #endregion
//    }

//    class BresSupport
//    {
//        private static readonly ushort[] HighestBitLut =
//        {
//            0xFFFF, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5,
//            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6,
//            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
//            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
//            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
//            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
//            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
//            7, 7, 7, 7, 7, 7
//        };

//        // http://wiki.tockdom.com/wiki/BRRES_Index_Group_(File_Format)#GNU_C_example
//        public static ushort CalculateId(string subjectName, string objectName)
//        {
//            if (objectName.Length < subjectName.Length)
//                return (ushort)(subjectName.Length - 1 << 3 | HighestBitLut[(byte)subjectName[^1]]);

//            var subLen = subjectName.Length;
//            while (subLen-- > 0)
//            {
//                var diff = objectName[subLen] ^ subjectName[subLen];
//                if (diff > 0)
//                    return (ushort)(subLen << 3 | HighestBitLut[diff]);
//            }

//            return 0xFFFF;
//        }
//    }
//}
