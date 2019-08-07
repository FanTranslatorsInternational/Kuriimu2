using System.IO;
using Kompression.IO;

namespace Kompression.LempelZiv.Decoders
{
    public class BackwardLz77Decoder : ILzDecoder
    {
        private readonly ByteOrder _byteOrder;

        public BackwardLz77Decoder(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];

            using (var inputReverseStream = new ReverseStream(input))
            using (var outputReverseStream = new ReverseStream(output))
            {
                inputReverseStream.Position = input.Length;
                inputReverseStream.Read(buffer, 0, 4);
                var originalBottom = _byteOrder == ByteOrder.LittleEndian ? GetLittleEndian(buffer) : GetBigEndian(buffer);
                inputReverseStream.Read(buffer, 0, 4);
                var bufferTopAndBottom = _byteOrder == ByteOrder.LittleEndian ? GetLittleEndian(buffer) : GetBigEndian(buffer);

                var sourcePosition = input.Length - (bufferTopAndBottom >> 24);
                var destinationPosition = input.Length + originalBottom;
                var endPosition = input.Length - (bufferTopAndBottom & 0xFFFFFF);

                inputReverseStream.Position = sourcePosition;
                outputReverseStream.Position = destinationPosition;

                var windowBuffer = new byte[0x1002];
                var windowBufferPosition = 0;
                var codeBlock = inputReverseStream.ReadByte();
                var codeBlockPosition = 8;
                while (inputReverseStream.Position - endPosition > 0)
                {
                    if (codeBlockPosition == 0)
                    {
                        codeBlock = inputReverseStream.ReadByte();
                        codeBlockPosition = 8;
                    }

                    var flag = (codeBlock >> --codeBlockPosition) & 0x1;
                    if (flag == 0)
                    {
                        var value = (byte)inputReverseStream.ReadByte();
                        outputReverseStream.WriteByte(value);
                        windowBuffer[windowBufferPosition++ % windowBuffer.Length] = value;
                    }
                    else
                    {
                        var byte1 = inputReverseStream.ReadByte();
                        var byte2 = inputReverseStream.ReadByte();

                        var length = (byte1 >> 4) + 3;
                        var displacement = (((byte1 & 0xF) << 8) | byte2) + 3;

                        var bufferIndex = windowBufferPosition + windowBuffer.Length - displacement;
                        for (var i = 0; i < length; i++)
                        {
                            var value = windowBuffer[bufferIndex++ % windowBuffer.Length];
                            outputReverseStream.WriteByte(value);
                            windowBuffer[windowBufferPosition++ % windowBuffer.Length] = value;
                        }
                    }
                }
            }
        }

        private int GetLittleEndian(byte[] data)
        {
            return data[3] | data[2] | data[1] | data[0];
        }

        private int GetBigEndian(byte[] data)
        {
            return data[0] | data[1] | data[2] | data[3];
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
