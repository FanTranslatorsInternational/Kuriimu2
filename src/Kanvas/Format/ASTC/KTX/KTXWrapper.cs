using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Text;
using Kanvas.Format.ASTC.KTX.Models;
using Kanvas.Models;

namespace Kanvas.Format.ASTC.KTX
{
    public class KTXWrapper : IDisposable
    {
        private readonly long _imgDataOffset;

        private Stream _stream;
        private BinaryReader _reader;
        private ByteOrder _byteOrder;

        private Header _header;
        private List<byte[]> _keyValueData;

        public KTXWrapper(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException(file);

            _stream = File.OpenRead(file);
            _reader = new BinaryReader(_stream, Encoding.ASCII, true);

            SetEndianess();

            ParseHeader();
            ParseKeyValueData();

            _imgDataOffset = _reader.BaseStream.Position;
        }

        private short ReadInt16() =>
            Kanvas.Support.Convert.FromByteArray<short>(_reader.ReadBytes(2), _byteOrder);
        private ushort ReadUInt16() =>
            Kanvas.Support.Convert.FromByteArray<ushort>(_reader.ReadBytes(2), _byteOrder);
        private int ReadInt32() =>
            Kanvas.Support.Convert.FromByteArray<int>(_reader.ReadBytes(4), _byteOrder);
        private uint ReadUInt32() =>
            Kanvas.Support.Convert.FromByteArray<uint>(_reader.ReadBytes(4), _byteOrder);

        private void SetEndianess()
        {
            _reader.BaseStream.Position = 0xC;

            _byteOrder = ReadInt32() == 0x04030201 ? ByteOrder.LittleEndian : ByteOrder.BigEndian;
        }

        private void ParseHeader()
        {
            _reader.BaseStream.Position = 0;
            _header = new Header
            {
                magic = Encoding.ASCII.GetString(_reader.ReadBytes(12)),
                endian = ReadInt32(),
                glType = (GLDataType)ReadInt32(),
                glTypeSize = ReadInt32(),
                glFormat = (GLFormat)ReadInt32(),
                glInternalFormat = (GLFormat)ReadInt32(),
                glBaseInternalFormat = (GLFormat)ReadInt32(),
                width = ReadInt32(),
                height = ReadInt32(),
                depth = ReadInt32(),
                arrayCount = ReadInt32(),
                faceCount = ReadInt32(),
                mipmapCount = ReadInt32(),
                keyValueDataSize = ReadInt32()
            };
        }

        private void ParseKeyValueData()
        {
            var headerLength = 0xC + 13 * 0x4;

            _reader.BaseStream.Position = headerLength;

            _keyValueData = new List<byte[]>();
            var limit = _header.keyValueDataSize;
            while (limit-- > 0)
            {
                var size = ReadInt32();
                _keyValueData.Add(_reader.ReadBytes(size));

                _reader.BaseStream.Position += 4 - _reader.BaseStream.Position % 4;
            }
        }

        public IEnumerable<Color> GetImageColors()
        {
            _reader.BaseStream.Position = _imgDataOffset;

            var ret = new List<List<Color>>();
            var size = ReadInt32();

            //for (int array = 0; array < ((_header.arrayCount == 0) ? 1 : _header.arrayCount); array++)
            //{
            //    for (int face = 0; face < _header.faceCount; face++)
            //    {
            //        for (int depth = 0; depth < ((_header.depth == 0) ? 1 : _header.depth); depth++)
            //        {
            for (int height = 0; height < ((_header.height == 0) ? 1 : _header.height); height++)
            {
                ret.Add(new List<Color>());
                for (int width = 0; width < _header.width; width++)
                {
                    ret[height].Add(ReadComponent());
                }
            }
            //        }
            //    }
            //}

            return ret.SelectMany(r => { r.Reverse(); return r; });
        }

        private Color ReadComponent()
        {
            switch (_header.glType)
            {
                case GLDataType.BYTE:
                    return ReadColor(() => Convert.ToInt32(_reader.ReadSByte()));
                case GLDataType.UBYTE:
                    return ReadColor(() => Convert.ToInt32(_reader.ReadByte()));
                case GLDataType.SHORT:
                    return ReadColor(() => Convert.ToInt32(ReadInt16()));
                case GLDataType.USHORT:
                    return ReadColor(() => Convert.ToInt32(ReadUInt16()));
                case GLDataType.INT:
                    return ReadColor(ReadInt32);
                case GLDataType.UINT:
                    return ReadColor(() => Convert.ToInt32(ReadUInt32()));
                // TODO: Implement floating point!
                //case GLDataType.FLOAT:
                //    return ReadColor(() => Convert.ToInt32(_reader.ReadSingle()));
                //case GLDataType.DOUBLE:
                //    return ReadColor(() => Convert.ToInt32(_reader.ReadDouble()));
                default:
                    throw new NotSupportedException($"Unsupported glType {_header.glType}.");
            }
        }

        private Color ReadColor(Func<int> readValue)
        {
            int r = 0, g = 0, b = 0;
            int a = 255;

            switch (_header.glFormat)
            {
                case GLFormat.RED:
                    r = readValue();
                    break;
                case GLFormat.GREEN:
                    g = readValue();
                    break;
                case GLFormat.BLUE:
                    b = readValue();
                    break;
                case GLFormat.ALPHA:
                    a = readValue();
                    break;
                case GLFormat.LUMINANCE:
                    r = g = b = readValue();
                    break;
                case GLFormat.LUMINANCE_ALPHA:
                    r = g = b = readValue();
                    a = readValue();
                    break;
                case GLFormat.RGB:
                    r = readValue();
                    g = readValue();
                    b = readValue();
                    break;
                case GLFormat.RGBA:
                    r = readValue();
                    g = readValue();
                    b = readValue();
                    a = readValue();
                    break;
                default:
                    throw new NotSupportedException($"Unsupported glFormat {_header.glFormat}.");
            }

            return Color.FromArgb(a, r, g, b);
        }

        public void Dispose()
        {
            _reader = null;
            _stream = null;
        }
    }
}
