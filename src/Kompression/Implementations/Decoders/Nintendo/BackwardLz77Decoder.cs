using System;
using System.Diagnostics;
using System.IO;
using Komponent.IO.Streams;
using Kompression.Extensions;
using Kompression.IO;
using Kontract.Kompression.Configuration;
using Kontract.Models.IO;

namespace Kompression.Implementations.Decoders.Nintendo
{
    public class BackwardLz77Decoder : IDecoder
    {
        private readonly ByteOrder _byteOrder;

        public BackwardLz77Decoder(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public void Decode(Stream input, Stream output)
        {
            // Check if enough space exists for a footer
            if (input.Length >= 8)
            {
                // Read footer
                var buffer = new byte[4];
                input.Position = input.Length - 8;

                input.Read(buffer, 0, 4);
                var bufferTopAndBottom = _byteOrder == ByteOrder.LittleEndian
                    ? buffer.GetInt32LittleEndian(0)
                    : buffer.GetInt32BigEndian(0);

                input.Read(buffer, 0, 4);
                var decompressedOffset = _byteOrder == ByteOrder.LittleEndian
                    ? buffer.GetInt32LittleEndian(0)
                    : buffer.GetInt32BigEndian(0);
                var decompressedSize = input.Length + decompressedOffset;

                var top = bufferTopAndBottom & 0xFFFFFF;
                var bottom = bufferTopAndBottom >> 24 & 0xFF;

                // Check footer integrity
                if (bottom >= 8 && bottom <= 8 + 3 && top >= bottom && top <= input.Length &&
                    decompressedSize >= input.Length + decompressedOffset)
                    ReadCompressedData(input, output, decompressedSize, decompressedSize, input.Length - bottom,
                        input.Length - top);
                else
                {
                    Debugger.Break();
                    throw new InvalidOperationException("Something went wrong.");
                }
            }
            else
            {
                Debugger.Break();
                throw new InvalidOperationException("Something went wrong.");
            }

            //using (var inputReverseStream = new ReverseStream(input, input.Length - footerLength))
            //using (var outputReverseStream = new ReverseStream(output, input.Length + decompressedOffset))
            //{
            //ReadCompressedData(input, output, decompressedSize, decompressedSize, input.Length - bottom, input.Length - top);
            //}
        }

        private void ReadCompressedData(Stream input, Stream output, long decompressedSize, long dest, long src, long end)
        {
            while (src - end > 0)
            {
                input.Position = --src;
                var flag = input.ReadByte();

                for (var i = 0; i < 8; i++)
                {
                    if (((flag << i) & 0x80) == 0)
                    {
                        if (dest - end < 1 || src - end < 1)
                        {
                            Debugger.Break();
                            throw new InvalidOperationException("Something went wrong.");
                        }

                        input.Position = --src;
                        var value = input.ReadByte();

                        output.Position = --dest;
                        output.WriteByte((byte)value);
                    }
                    else
                    {
                        if (src - end < 2)
                        {
                            Debugger.Break();
                            throw new InvalidOperationException("Something went wrong.");
                        }

                        input.Position = --src;
                        var size = input.ReadByte();

                        input.Position = --src;
                        var offset = (((size & 0x0F) << 8) | input.ReadByte()) + 3;
                        size = ((size >> 4) & 0x0F) + 3;

                        if(dest<0x60)
                            Debugger.Break();

                        if (size > dest - end)
                        {
                            Debugger.Break();
                            throw new InvalidOperationException("Something went wrong.");
                        }

                        var data = dest + offset;
                        if (data > decompressedSize)
                        {
                            Debugger.Break();
                            throw new InvalidOperationException("Something went wrong.");
                        }

                        for (var j = 0; j < size; j++)
                        {
                            output.Position = --data;
                            var value = output.ReadByte();

                            output.Position = --dest;
                            output.WriteByte((byte)value);
                        }
                    }

                    if (src - end <= 0)
                        break;
                }
            }

            // Copy remaining bytes after end
            input.Position = 0;
            output.Position = 0;

            var buffer = new byte[end];
            input.Read(buffer);
            output.Write(buffer);

            //var circularBuffer = new CircularBuffer(0x1002);

            //var codeBlock = input.ReadByte();
            //var codeBlockPosition = 8;
            //while (input.Position < endPosition)
            //{
            //    if (codeBlockPosition == 0)
            //    {
            //        codeBlock = input.ReadByte();
            //        codeBlockPosition = 8;
            //    }

            //    var flag = (codeBlock >> --codeBlockPosition) & 0x1;
            //    if (flag == 0)
            //        HandleUncompressedBlock(input, output, circularBuffer);
            //    else
            //        HandleCompressedBlock(input, output, circularBuffer);
            //}

            //while (input.Position < input.Length)
            //    output.WriteByte((byte)input.ReadByte());
        }

        private void HandleUncompressedBlock(Stream input, Stream output, CircularBuffer circularBuffer)
        {
            var next = input.ReadByte();

            output.WriteByte((byte)next);
            circularBuffer.WriteByte((byte)next);
        }

        private void HandleCompressedBlock(Stream input, Stream output, CircularBuffer circularBuffer)
        {
            var byte1 = input.ReadByte();
            var byte2 = input.ReadByte();

            var length = (byte1 >> 4) + 3;
            var displacement = (((byte1 & 0xF) << 8) | byte2) + 3;

            circularBuffer.Copy(output, displacement, length);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
