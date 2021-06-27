﻿using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Komponent.IO.Attributes;
using Komponent.IO.Streams;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_headstrong_games.Archives
{
    class FabNodeHeader
    {
        [FixedLength(4)]
        public string magic;
        public int size;
        [FixedLength(4)]
        public string description;
    }

    class FabNode
    {
        private FabNodeHeader _header;
        private int _headerLength = 0xC;

        public IList<FabNode> Nodes { get; private set; } = new List<FabNode>();

        public Stream Data { get; set; }

        public string Type => _header.magic;
        public string SubType => _header.description;

        public static FabNode Read(BinaryReaderX br)
        {
            // Read header
            var header = br.ReadType<FabNodeHeader>();

            var result = new FabNode { _header = header };
            switch (header.magic)
            {
                case "FBRC":
                    switch (header.description)
                    {
                        case "BNDL":
                            var numNode = Read(br);
                            var count = numNode.AsInt32();

                            result.Nodes.Add(numNode);
                            for (var i = 0; i < count; i++)
                            {
                                result.Nodes.Add(Read(br));
                            }
                            break;

                        case "TXTR":
                            result.Nodes.Add(Read(br));
                            result.Nodes.Add(Read(br));
                            result.Nodes.Add(Read(br));
                            break;
                    }
                    break;

                case "USER":
                    var offset1 = br.BaseStream.Position;
                    br.BaseStream.Position += (header.size - 4 + 1) & ~1;
                    result.Data = new SubStream(br.BaseStream, offset1, header.size - 4);
                    break;

                case "LIST":
                    switch (header.description)
                    {
                        case "FILE":
                            result.Nodes.Add(Read(br));
                            result.Nodes.Add(Read(br));
                            break;

                        case "META":
                            result.Nodes.Add(Read(br));
                            result.Nodes.Add(Read(br));
                            result.Nodes.Add(Read(br));
                            result.Nodes.Add(Read(br));
                            result.Nodes.Add(Read(br));
                            break;

                        case "DATA":
                            var nextHeader = br.ReadType<FabNodeHeader>();
                            br.BaseStream.Position -= 0xC;

                            // Specially handle USER node to unwrap the "actual" file data from it
                            if (nextHeader.magic == "USER")
                                result.Nodes.Add(Read(br));
                            else
                            {
                                var offset2 = br.BaseStream.Position;
                                br.BaseStream.Position += (header.size - 4 + 1) & ~1;
                                result.Data = new SubStream(br.BaseStream, offset2, header.size - 4);
                            }
                            break;
                    }
                    break;

                // By default treat content as data
                default:
                    var offset = br.BaseStream.Position - 4;
                    br.BaseStream.Position += (header.size - 4 + 1) & ~1;

                    result._headerLength = 0x8;
                    result.Data = new SubStream(br.BaseStream, offset, header.size);
                    break;
            }

            return result;
        }

        public void Write(BinaryWriterX bw)
        {
            var startPosition = bw.BaseStream.Position;
            bw.BaseStream.Position += _headerLength;

            // Write child nodes and data
            if (Data != null)
            {
                Data.Position = 0;
                Data.CopyTo(bw.BaseStream);

                // Update header
                _header.size = (int)(bw.BaseStream.Position - startPosition - 8);

                bw.WriteAlignment(2);
            }
            else
            {
                foreach (var node in Nodes)
                    node.Write(bw);

                // Update header
                _header.size = (int)(bw.BaseStream.Position - startPosition - 8);
            }

            var currentPosition = bw.BaseStream.Position;

            // Write header
            bw.BaseStream.Position = startPosition;
            bw.WriteString(_header.magic, Encoding.ASCII, false, false);
            bw.Write(_header.size);
            if (_headerLength > 8)
                bw.WriteString(_header.description, Encoding.ASCII, false, false);

            bw.BaseStream.Position = currentPosition;
        }

        public int AsInt32()
        {
            if (Data == null || Data.Length < 4)
                throw new InvalidOperationException("Data cannot be interpreted as Int32.");

            Data.Position = 0;
            var buffer = new byte[4];
            Data.Read(buffer, 0, 4);

            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        public string AsString()
        {
            if (Data == null)
                throw new InvalidOperationException("Data cannot be interpreted as a String.");

            Data.Position = 0;
            var buffer = new byte[Data.Length];
            Data.Read(buffer, 0, buffer.Length);

            return Encoding.ASCII.GetString(buffer);
        }
    }

    class FabArchiveFileInfo : ArchiveFileInfo
    {
        public FabNode DataNode { get; set; }

        public FabArchiveFileInfo(Stream fileData, string filePath) : base(fileData, filePath)
        {
        }

        public FabArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize) : base(fileData, filePath, configuration, decompressedSize)
        {
        }
    }
}
