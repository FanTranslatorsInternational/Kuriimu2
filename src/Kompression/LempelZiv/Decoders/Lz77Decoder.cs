using System.IO;
using Kompression.IO;

namespace Kompression.LempelZiv.Decoders
{
    class Lz77Decoder:ILzDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var windowBuffer = new byte[0xFF];
            var windowBufferOffset = 0;

            var bitReader = new BitReader(input, BitOrder.LSBFirst, 1, ByteOrder.BigEndian);
            while (bitReader.Length - bitReader.Position >= 9)
            {
                if (bitReader.ReadBit() == 0)
                {
                    var nextByte = (byte)bitReader.ReadByte();

                    output.WriteByte(nextByte);
                    windowBuffer[windowBufferOffset] = nextByte;
                    windowBufferOffset = (windowBufferOffset + 1) % windowBuffer.Length;
                }
                else
                {
                    var displacement = bitReader.ReadByte();
                    var length = bitReader.ReadByte();
                    var nextByte = (byte)bitReader.ReadByte();

                    var bufferIndex = windowBufferOffset + windowBuffer.Length - displacement;
                    for (var i = 0; i < length; i++)
                    {
                        var next = windowBuffer[bufferIndex++ % windowBuffer.Length];
                        output.WriteByte(next);
                        windowBuffer[windowBufferOffset] = next;
                        windowBufferOffset = (windowBufferOffset + 1) % windowBuffer.Length;
                    }

                    // If an occurrence goes until the end of the file, 'next' still exists
                    // In this case, 'next' shouldn't be written to the file
                    // but there is no indicator if this symbol has to be written or not
                    // According to Kuriimu, I once implemented a 0x24 static symbol for some reason
                    // Maybe 'next' is 0x24 if an occurrence goes directly until the end of a file?
                    // TODO: Fix overflowing symbol
                    // HINT: Look at Kuriimu issue 517 and see if the used compression is this one
                    output.WriteByte(nextByte);
                    windowBuffer[windowBufferOffset] = nextByte;
                    windowBufferOffset = (windowBufferOffset + 1) % windowBuffer.Length;
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
