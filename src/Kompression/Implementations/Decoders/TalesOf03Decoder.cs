using System;
using System.Buffers.Binary;
using System.IO;
using Kompression.Extensions;
using Kompression.IO;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class TalesOf03Decoder : IDecoder
    {
        private const int PreBufferSize_ = 0xFEF;

        public void Decode(Stream input, Stream output)
        {
            if (input.ReadByte() != 0x03)
                throw new InvalidOperationException("This is not a tales of compression with version 3.");

            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            var compressedDataSize = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(0));
            input.Read(buffer, 0, 4);
            var decompressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(0));

            var circularBuffer=new CircularBuffer(0x1000)
            {
                Position = PreBufferSize_
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
                    circularBuffer.WriteByte(value);
                }
                else
                {
                    // compressed data
                    var byte1 = input.ReadByte();
                    var byte2 = input.ReadByte();

                    if ((byte2 & 0xF) != 0xF)
                    {
                        // LZ compressed data
                        var length = (byte2 & 0xF) + 3;
                        var bufferPosition = byte1 | ((byte2 & 0xF0) << 4);

                        // Convert buffer position to displacement
                        var displacement = (circularBuffer.Position - bufferPosition) % circularBuffer.Length;
                        displacement = (displacement + circularBuffer.Length) % circularBuffer.Length;
                        if (displacement == 0)
                            displacement = 0x1000;

                        circularBuffer.Copy(output,displacement,length);
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
                            circularBuffer.WriteByte(repValue);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
