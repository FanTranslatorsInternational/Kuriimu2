//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using Kanvas;
//using Komponent.IO;
//using Komponent.IO.Attributes;
//using Kontract.Kanvas;
//using Kontract.Models.Image;
//using Kontract.Models.IO;

//namespace plugin_nintendo.Images
//{
//    class CgfxHeader
//    {
//        [FixedLength(4)]
//        public string magic = "CGFX";

//        [Endianness(ByteOrder = ByteOrder.BigEndian)]
//        public ByteOrder byteOrder;

//        public short headerSize;
//        public int revision;
//        public int fileSize;
//        public int entryCount;
//    }

//    #region Data section

//    class CgfxData
//    {
//        private byte[] _stringRegion;

//        public CgfxDataHeader Header { get; }

//        public IReadOnlyList<CgfxDict> Data { get; }

//        private CgfxData(CgfxDataHeader header, IReadOnlyList<CgfxDict> data, byte[] stringRegion)
//        {
//            Header = header;
//            Data = data;

//            _stringRegion = stringRegion;
//        }

//        public static CgfxData Read(BinaryReaderX br)
//        {
//            var startPosition = br.BaseStream.Position;

//            // Read header
//            var header = br.ReadType<CgfxDataHeader>();

//            // Read section infos
//            var sectionInfos = new List<CgfxSectionInfo>();
//            for (var i = 0; i < 15; i++)
//            {
//                var sectionInfo = br.ReadType<CgfxSectionInfo>();
//                if (sectionInfo.offset > 0)
//                    sectionInfo.offset += (int)(br.BaseStream.Position - 4);

//                sectionInfos.Add(sectionInfo);
//            }

//            // Read section sections
//            var dicts = new List<CgfxDict>();
//            foreach (var sectionInfo in sectionInfos)
//            {
//                if (sectionInfo.offset <= 0)
//                {
//                    dicts.Add(null);
//                    continue;
//                }

//                br.BaseStream.Position = sectionInfo.offset;
//                dicts.Add(CgfxDict.Read(br));
//            }

//            // Read data for dict nodes
//            var dictNodes = EnumerateDictNodes(dicts).ToArray();

//            var minStringOffset = dictNodes.Min(x => x.Item2.nodeNameOffset);
//            for (var i = 0; i < dictNodes.Length; i++)
//            {
//                var node = dictNodes[i];

//                // Determine length
//                var nextOffset = i + 1 != dictNodes.Length ? dictNodes[i + 1].Item2.nodeDataOffset : minStringOffset;
//                var length = nextOffset - node.Item2.nodeDataOffset;

//                // Read data
//                br.BaseStream.Position = node.Item2.nodeDataOffset;
//                node.Item2.Data = ParseData(br, node.Item1, length);
//            }

//            // Read string region
//            br.BaseStream.Position = minStringOffset;
//            var stringRegion = br.ReadBytes((int)(header.size + startPosition - minStringOffset));

//            return new CgfxData(header, dicts, stringRegion);
//        }

//        private static IEnumerable<(int, CgfxDictNode)> EnumerateDictNodes(IList<CgfxDict> sections)
//        {
//            for (var i = 0; i < sections.Count; i++)
//            {
//                var section = sections[i];
//                if (section == null)
//                    continue;

//                foreach (var node in section.Nodes)
//                    yield return (i, node);
//            }
//        }

//        private static object ParseData(BinaryReaderX br, int section, int length)
//        {
//            switch (section)
//            {
//                // TXOB
//                case 1:
//                    return Txob.Read(br);

//                default:
//                    return br.ReadBytes(length);
//            }
//        }
//    }

//    class CgfxDict
//    {
//        public CgfxDictHeader Header { get; }

//        // TODO: Make tree after knowing how the nodes relate to each other
//        public CgfxDictNode Root { get; }

//        public IList<CgfxDictNode> Nodes { get; }

//        private CgfxDict(CgfxDictHeader header, CgfxDictNode root, IList<CgfxDictNode> nodes)
//        {
//            Header = header;
//            Root = root;
//            Nodes = nodes;
//        }

//        public static CgfxDict Read(BinaryReaderX br)
//        {
//            // Read header
//            var header = br.ReadType<CgfxDictHeader>();

//            // Read root node
//            var root = br.ReadType<CgfxDictNode>();

//            // Read sub nodes
//            var nodes = new List<CgfxDictNode>();
//            for (var i = 0; i < header.entryCount; i++)
//            {
//                nodes.Add(br.ReadType<CgfxDictNode>());

//                nodes.Last().nodeNameOffset += (int)(br.BaseStream.Position - 8);
//                nodes.Last().nodeDataOffset += (int)(br.BaseStream.Position - 4);

//                var endPosition = br.BaseStream.Position;

//                // Read name
//                br.BaseStream.Position = nodes.Last().nodeNameOffset;
//                nodes.Last().Name = br.ReadCStringASCII();

//                // HINT: Reading data happens after the whole DICT is read.

//                br.BaseStream.Position = endPosition;
//            }

//            return new CgfxDict(header, root, nodes);
//        }
//    }

//    class CgfxDataHeader
//    {
//        [FixedLength(4)]
//        public string magic = "DATA";
//        public int size;
//    }

//    class CgfxSectionInfo
//    {
//        public int entryCount;
//        public int offset;
//    }

//    class CgfxDictHeader
//    {
//        [FixedLength(4)]
//        public string magic = "DICT";
//        public int dictSize;
//        public int entryCount;
//    }

//    [DebuggerDisplay("{Name}")]
//    class CgfxDictNode
//    {
//        public uint nodeReference;
//        public short nodeLeft;
//        public short nodeRight;
//        public int nodeNameOffset;
//        public int nodeDataOffset;

//        public string Name { get; set; }

//        public object Data { get; set; }
//    }

//    #endregion

//    #region Texture object

//    class Txob
//    {
//        public TxobData1 Data1 { get; }
//        public CgfxDict UserData { get; }
//        public TxobData2 Data2 { get; }

//        private Txob(TxobData1 data1, TxobData2 data2, CgfxDict userData)
//        {
//            Data1 = data1;
//            Data2 = data2;
//            UserData = userData;
//        }

//        public static Txob Read(BinaryReaderX br)
//        {
//            var startPosition = br.BaseStream.Position;

//            // Read data 1
//            var data1 = br.ReadType<TxobData1>();

//            // Read optional user data
//            CgfxDict userDict = null;
//            if (data1.userDataOffset != 0 && data1.userDataSize > 4)
//            {
//                br.BaseStream.Position = startPosition + 0x14 + data1.userDataOffset;
//                userDict = CgfxDict.Read(br);
//            }

//            // Read data 2
//            var data2 = br.ReadType<TxobData2>();

//            return new Txob(data1, data2, userDict);
//        }
//    }

//    class TxobData1
//    {
//        public int type;
//        [FixedLength(4)]
//        public string magic = "TXOB";
//        public int revision;
//        public int nameOffset;

//        public int userDataEntries;
//        public int userDataOffset;
//        public int height;
//        public int width;

//        public int openGLFormat;
//        public int openGLType;
//        public int mipCount;
//        public int texObject;

//        public int locationFlags;
//        public int format;
//        public int userDataSize;
//    }

//    class TxobData2
//    {
//        public int height2;
//        public int width2;
//        public int texDataSize;
//        public int texDataOffset;

//        public int dynamicAllocator;
//        public int bitDepth;
//        public int locAddress;
//        public int memAddress;
//    }

//    #endregion

//    class CgfxSupport
//    {
//        private static readonly IDictionary<uint, IColorEncoding> TxobFormats = new Dictionary<uint, IColorEncoding>
//        {
//            // composed of dataType and PixelFormat
//            // short+short
//            [0x14016752] = ImageFormats.Rgba8888(),
//            [0x80336752] = ImageFormats.Rgba4444(),
//            [0x80346752] = ImageFormats.Rgba5551(),
//            [0x14016754] = ImageFormats.Rgb888(),
//            [0x83636754] = ImageFormats.Rgb565(),
//            [0x14016756] = ImageFormats.A8(),
//            [0x67616756] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst),
//            [0x14016757] = ImageFormats.L8(),
//            [0x67616757] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
//            [0x67606758] = ImageFormats.La44(),
//            [0x14016758] = ImageFormats.La88(),
//            [0x0000675A] = ImageFormats.Etc1(true),
//            [0x0000675B] = ImageFormats.Etc1A4(true),
//            [0x1401675A] = ImageFormats.Etc1(true),
//            [0x1401675B] = ImageFormats.Etc1A4(true)
//        };

//        public static EncodingDefinition GetEncodingDefinition()
//        {
//            var definition = new EncodingDefinition();
//            definition.AddColorEncodings(TxobFormats.ToDictionary(x => (int)x.Key, y => y.Value));

//            return definition;
//        }
//    }
//}
