using System;
using System.Diagnostics;
using System.IO;
using Kompression.Configuration;
using Kompression.IO;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    // TODO: Write TalesOf03Encoder
    public class TalesOf03Decoder : IDecoder
    {
        private CircularBuffer _circularBuffer;
        private int _preBufferSize;

        public TalesOf03Decoder(int preBufferSize)
        {
            _preBufferSize = preBufferSize;
        }

        public void Decode(Stream input, Stream output)
        {
            if (input.ReadByte() != 0x03)
                throw new InvalidOperationException("This is not a tales of compression with version 3.");

            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            var compressedDataSize = GetLittleEndian(buffer);
            input.Read(buffer, 0, 4);
            var decompressedSize = GetLittleEndian(buffer);

            _circularBuffer=new CircularBuffer(0x1000)
            {
                Position = _preBufferSize
            };

            var flags = 0;
            var flagPosition = 8;
            while (output.Length < decompressedSize)
            {
                if (flagPosition == 8)
                {
                    flagPosition = 0;
                    flags = input.ReadByte();
                }

                if (((flags >> flagPosition++) & 0x1) == 1)
                {
                    // raw data
                    var value = (byte)input.ReadByte();

                    output.WriteByte(value);
                    _circularBuffer.WriteByte(value);
                }
                else
                {
                    if (output.Position > 0x3c700)
                        Debugger.Break();

                    // compressed data
                    var byte1 = input.ReadByte();
                    var byte2 = input.ReadByte();

                    if ((byte2 & 0xF) != 0xF)
                    {
                        // LZ compressed data
                        var length = (byte2 & 0xF) + 3;
                        var bufferPosition = byte1 | ((byte2 & 0xF0) << 4);

                        // Convert buffer position to displacement
                        var displacement = (_circularBuffer.Position - bufferPosition) % _circularBuffer.Length;
                        displacement = (displacement + _circularBuffer.Length) % _circularBuffer.Length;
                        if (displacement == 0)
                            displacement = 0x1000;

                        _circularBuffer.Copy(output,displacement,length);
                    }
                    else
                    {
                        // RLE compressed data
                        byte repValue;
                        var length = (byte2 & 0xF0) >> 4;
                        if (length == 0)
                        {
                            repValue = (byte)input.ReadByte();
                            length = byte1 + 0x13;
                        }
                        else
                        {
                            repValue = (byte)byte1;
                            length += 3;
                        }

                        for (var i = 0; i < length; i++)
                        {
                            output.WriteByte(repValue);
                            _circularBuffer.WriteByte(repValue);
                        }
                    }
                }
            }
        }

        private int GetLittleEndian(byte[] data)
        {
            return (data[3] << 24) | (data[2] << 16) | (data[1] << 8) | data[0];
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
