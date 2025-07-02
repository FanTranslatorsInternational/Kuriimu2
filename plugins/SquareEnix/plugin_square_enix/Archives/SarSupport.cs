namespace plugin_square_enix.Archives
{
    class SarEntry
    {
        public int offset;
        public int size;
    }

    class SarContainerHeader
    {
        public string magic;
        public int data1;
        public int data2;
    }

    class SarSupport
    {
        public static int GetCompressedSize(Stream input, int offset, int readCompSize)
        {
            var buffer = new byte[4];
            ReadOnlySpan<byte> magic = "~lz7"u8;

            var startPos = input.Position;
            var checkPos = offset + readCompSize;

            input.Position = checkPos;
            input.Read(buffer, 0, 4);
            while (!magic.SequenceEqual(buffer))
            {
                checkPos++;

                input.Position = checkPos;
                input.Read(buffer, 0, 4);
            }

            input.Position = startPos;
            return checkPos - offset;
        }

        // This code is correct as seen here. It counts all bits necessary for a LZ10 compression.
        // This may return a bit count not dividable by 8.
        public static int CalculateBits(Stream input)
        {
            var bits = 0;

            input.Position = 4;
            while (input.Position < input.Length)
            {
                var flag = input.ReadByte();

                for (var i = 7; i >= 0; i--)
                {
                    if (input.Position >= input.Length)
                        break;

                    bits++;
                    if (((flag >> i) & 1) == 0)
                    {
                        input.Position++;
                        bits += 8;
                    }
                    else
                    {
                        input.Position += 2;
                        bits += 16;
                    }
                }
            }

            return bits;
        }
    }
}
