using System.IO;

namespace Kompression.LempelZiv.Encoders
{
    class LzssEncoder : ILzEncoder
    {
        public void Encode(Stream input, Stream output, Match[] matches)
        {
            var outputStartPos = output.Position;
            output.Position += 0x10;
            new Lz10Encoder().WriteCompressedData(input, output, matches);

            var outputPos = output.Position;
            output.Position = outputStartPos;
            output.Write(new byte[] { 0x53, 0x53, 0x5A, 0x4C }, 0, 4);
            output.Position += 8;
            var decompressedSizeBuffer = new[]
            {
                (byte)input.Length,
                (byte)((input.Length>>8)&0xFF),
                (byte)((input.Length>>16)&0xFF),
                (byte)((input.Length>>24)&0xFF),
            };
            output.Write(decompressedSizeBuffer, 0, 4);

            output.Position = outputPos;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
