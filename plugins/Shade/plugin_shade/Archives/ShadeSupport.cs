using System.Text;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_shade.Archives
{
    static class ShadeSupport
    {
        public static string GuessExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Contains(0x55AA382D))
                return "arc";

            if (magicSamples.Contains(0x52415344))
                return "rasd";

            if (magicSamples.Contains(0x53485458))
                return "shtx";

            if (magicSamples.Contains(0x53534144))
                return "ssad";

            if (magicSamples.Contains(0x434d504b))
                return "cpmk";

            if (magicSamples.Contains(StringToUInt32("bres")))
                return "bres";

            return "bin";
        }

        private static uint StringToUInt32(string text)
        {
            return BufferToUInt32(Encoding.UTF8.GetBytes(text));
        }

        private static IList<uint> CollectMagicSamples(Stream input)
        {
            var bkPos = input.Position;

            // Get 3 samples to check magic with compression
            input.Position = bkPos;
            var magic1 = PeekUInt32(input);
            input.Position = bkPos + 1;
            var magic2 = PeekUInt32(input);
            input.Position = bkPos + 2;
            var magic3 = PeekUInt32(input);

            return [magic1, magic2, magic3];
        }

        private static uint PeekUInt32(Stream input)
        {
            var bkPos = input.Position;

            var buffer = new byte[4];
            input.Read(buffer, 0, 4);

            input.Position = bkPos;

            return BufferToUInt32(buffer);
        }

        private static uint BufferToUInt32(byte[] buffer)
        {
            return (uint)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
        }

        public static string CreateFileName(int index, Stream input, bool isCompressed)
        {
            input.Position = isCompressed ? 0xC : 0;
            var extension = GuessExtension(input);

            return $"{index:00000000}.{extension}";
        }

        public static long PeekDecompressedSize(Stream stream)
        {
            var bkPos = stream.Position;

            stream.Position = 4;
            var decompressedSize = PeekInt32LittleEndian(stream);

            stream.Position = bkPos;
            return decompressedSize;
        }

        public static uint PeekInt32LittleEndian(Stream input)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 4);
            input.Position -= 4;

            return (uint)((buffer[3] << 24) | (buffer[2] << 16) | (buffer[1] << 8) | buffer[0]);
        }
    }
    class ShadeArchiveFile : ArchiveFile
    {
        public long OriginalSize { get; }

        public ShadeArchiveFile(ArchiveFileInfo fileInfo) : base(fileInfo)
        {
            OriginalSize = fileInfo.FileData.Length;
        }

        public long WriteFileData(Stream output)
        {
            var writtenSize = WriteFileData(output, true);

            if (writtenSize > OriginalSize)
                throw new InvalidOperationException("The replaced file cannot be larger than its original.");

            // Pad to original size
            var paddedSize = OriginalSize - writtenSize;
            if (paddedSize > 0)
            {
                var padding = new byte[paddedSize];
                output.Write(padding, 0, padding.Length);

                writtenSize += paddedSize;
            }

            // Return padded size as written
            return writtenSize;
        }
    }
}
