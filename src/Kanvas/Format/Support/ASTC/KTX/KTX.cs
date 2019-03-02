using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

using Komponent.IO;
using Komponent.IO.Attributes;

namespace Kanvas.Support.ASTC
{
    public enum GLDataType : int
    {
        BYTE = 0x1400,
        UBYTE = 0x1401,
        SHORT = 0x1402,
        USHORT = 0x1403,
        INT = 0x1404,
        UINT = 0x1405,
        FLOAT = 0x1406,
        TWOBYTES = 0x1407,
        THREEBYTES = 0x1408,
        FOURBYTES = 0x1409,
        DOUBLE = 0x140A,
        //HALF_FLOAT = 0x140B,
        FIXED = 0x140C
    }

    public enum GLFormat : int
    {
        RED = 0x1903,
        GREEN = 0x1904,
        BLUE = 0x1905,
        ALPHA = 0x1906,
        RGB = 0x1907,
        RGBA = 0x1908,
        LUMINANCE = 0x1909,
        LUMINANCE_ALPHA = 0x190A
    }

    public class KTX : IDisposable
    {
        private class KTXHeader
        {
            [FixedLength(12)]
            public string magic;
            public int endian;
            public GLDataType glType;
            public int glTypeSize;
            public GLFormat glFormat;
            public GLFormat glInternalFormat;
            public GLFormat glBaseInternalFormat;
            public int width;
            public int height;
            public int depth;
            public int arrayCount;
            public int faceCount;
            public int mipmapCount;
            public int keyValueDataSize;
        }

        Stream _stream;
        BinaryReaderX _reader;

        KTXHeader _header;
        List<byte[]> _keyValueData;
        long _imgDataOffset;

        ByteOrder _byteOrder;

        public KTX(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException(file);

            _stream = File.OpenRead(file);
            _reader = new BinaryReaderX(_stream, true);

            GetEndianess();
            _reader.ByteOrder = _byteOrder;

            ParseHeader();
            ParseKeyValueData();
            _imgDataOffset = _reader.BaseStream.Position;
        }

        private void GetEndianess()
        {
            _reader.BaseStream.Position = 0xC;
            var check = _reader.ReadInt32();

            if (check == 0x04030201)
                _byteOrder = ByteOrder.LittleEndian;
            else
                _byteOrder = ByteOrder.BigEndian;
        }

        private void ParseHeader()
        {
            using (var br = new BinaryReaderX(_stream, true, _byteOrder))
            {
                br.BaseStream.Position = 0;
                _header = br.ReadStruct<KTXHeader>();
            }
        }

        private void ParseKeyValueData()
        {
            var headerLength = 0xC + 13 * 0x4;

            using (var br = new BinaryReaderX(_stream, true, _byteOrder))
            {
                br.BaseStream.Position = headerLength;

                _keyValueData = new List<byte[]>();
                var limit = _header.keyValueDataSize;
                while (limit > 0)
                {
                    var size = br.ReadInt32();
                    _keyValueData.Add(br.ReadBytes(size));
                    br.SeekAlignment(4);
                }
            }
        }

        public IEnumerable<Color> GetImageColors()
        {
            _reader.BaseStream.Position = _imgDataOffset;

            var ret = new List<List<Color>>();
            var size = _reader.ReadInt32();

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
                    return ReadColor(() => Convert.ToInt32(_reader.ReadInt16()));
                case GLDataType.USHORT:
                    return ReadColor(() => Convert.ToInt32(_reader.ReadUInt16()));
                case GLDataType.INT:
                    return ReadColor(() => Convert.ToInt32(_reader.ReadInt32()));
                case GLDataType.UINT:
                    return ReadColor(() => Convert.ToInt32(_reader.ReadUInt32()));
                case GLDataType.FLOAT:
                    return ReadColor(() => Convert.ToInt32(_reader.ReadSingle()));
                case GLDataType.DOUBLE:
                    return ReadColor(() => Convert.ToInt32(_reader.ReadDouble()));
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
