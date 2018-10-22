using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using plugin_valkyria_chronicles.HTEX;

namespace plugin_valkyria_chronicles.MLX0
{
    public sealed class MLX0
    {
        /// <summary>
        /// Streams of the contained images.
        /// </summary>
        public List<(Stream Image, string Name)> ImageStreams { get; set; } = new List<(Stream Image, string Name)>();

        #region InstanceData

        private PacketHeader _packetHeader;
        private int _chunkCount;
        private List<IzcaChunk> _izcaChunks;
        private List<List<int>> _izcaChunkOffsets;

        private PacketHeader _mlx0PacketHeader; // No Changes
        private byte[] _mlx0Data; // No Changes

        private PacketHeader _mxtlPacketHeader; // No Changes
        private byte[] _mxtlData; // No Changes

        private PacketHeaderX _hsprPacketHeader; // No Changes
        private byte[] _hsprData; // No Changes

        private PacketHeader _mxtfPacketHeader; // No Changes
        private byte[] _mxtfData; // No Changes

        private List<PacketHeader> _htsfPacketHeaders = new List<PacketHeader>();
        private List<HtsfHeader> _htsfHeaders = new List<HtsfHeader>();

        private PacketHeader _izcaFooter;

        #endregion

        /// <summary>
        /// Read an MLX0 file into memory.
        /// </summary>
        /// <param name="input">A readable stream of an MLX0 file.</param>
        public MLX0(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                // Packet Header IZCA
                _packetHeader = br.ReadStruct<PacketHeader>();

                // IZCA Header
                _chunkCount = br.ReadInt32();
                br.ReadInt32(); // Zero

                _izcaChunks = br.ReadMultiple<IzcaChunk>(_chunkCount);

                _izcaChunkOffsets = new List<List<int>>();
                foreach (var chunk in _izcaChunks)
                {
                    var chunkOffsets = new List<int>();
                    br.BaseStream.Position = chunk.Offset;
                    chunkOffsets.AddRange(br.ReadMultiple<int>(chunk.Count));
                    _izcaChunkOffsets.Add(chunkOffsets);
                }

                var index = 0;
                var strings = new List<string>();
                foreach (var list in _izcaChunkOffsets)
                    foreach (var offset in list)
                    {
                        br.BaseStream.Position = offset;
                        var peek = br.PeekString(4);

                        switch (peek)
                        {
                            case "MLX0":
                                _mlx0PacketHeader = br.ReadStruct<PacketHeader>();
                                _mlx0Data = br.ReadBytes(_mlx0PacketHeader.PacketSize);
                                break;
                            case "MXTL": // Image Names~
                                _mxtlPacketHeader = br.ReadStruct<PacketHeader>();
                                _mxtlData = br.ReadBytes(_mxtlPacketHeader.PacketSize);
                                br.ReadInt32(); // Unk1
                                var stringStart = br.BaseStream.Position;
                                br.ReadInt32(); // Unk2
                                var stringCount = br.ReadInt32();
                                var stringMeta = br.ReadMultiple<MxtlChunk>(stringCount);
                                foreach (var meta in stringMeta)
                                {
                                    br.BaseStream.Position = stringStart + meta.Offset;
                                    strings.Add(br.ReadCStringASCII());
                                }
                                break;
                            case "HSPR":
                                _hsprPacketHeader = br.ReadStruct<PacketHeaderX>();
                                _hsprData = br.ReadBytes(_hsprPacketHeader.PacketSize);
                                break;
                            case "MXTF":
                                _mxtfPacketHeader = br.ReadStruct<PacketHeader>();
                                _mxtfData = br.ReadBytes(_mxtfPacketHeader.PacketSize);
                                break;
                            case "HTSF":
                                var htsfPacketHeader = br.ReadStruct<PacketHeader>();
                                _htsfPacketHeaders.Add(htsfPacketHeader);
                                var htsfHeader = br.ReadStruct<HtsfHeader>();
                                _htsfHeaders.Add(htsfHeader);
                                ImageStreams.Add((new MemoryStream(br.ReadBytes(htsfPacketHeader.PacketSize - Common.PacketHeaderXSize)), strings[index]));
                                index++;
                                break;
                        }
                    }

                // Footers
                _izcaFooter = br.ReadStruct<PacketHeader>();
            }
        }

        /// <summary>
        /// Write an MLX0 file to disk.
        /// </summary>
        /// <param name="output">A writable stream of an MLX0 file.</param>
        public void Save(Stream output)
        {
            using (var bw = new BinaryWriterX(output))
            {
                bw.BaseStream.Position = Common.PacketHeaderXSize * 3;



                // Footers
                _packetHeader.PacketSize = (int)bw.BaseStream.Position - Common.PacketHeaderSize;
                bw.WriteStruct(_izcaFooter);

                // Write Packet Header
                bw.BaseStream.Position = 0;
                bw.WriteStruct(_packetHeader);

                // Write HTSF Packet Header
                bw.WriteStruct(_htsfPacketHeaders);

                // Write HTSF Header
                bw.WriteStruct(_htsfHeaders);
            }
        }
    }
}
