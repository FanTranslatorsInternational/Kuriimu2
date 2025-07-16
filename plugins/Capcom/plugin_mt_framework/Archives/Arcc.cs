using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_mt_framework.Archives
{
    class Arcc
    {
        private static readonly int HeaderSize = 0x8;
        private static readonly int EntrySize = 0x50;

        private MtHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = MtArcSupport.ReadHeader(br);

            // Read entries
            var key = GetCipherKey("imaguy_uyrag_igurustim_", "enokok_ikorodo_odohuran");
            var entryStream = new MtBlowfishStream(new SubStream(input, HeaderSize, _header.entryCount * EntrySize), key);

            using var entryBr = new BinaryReaderX(entryStream);
            var entries = MtArcSupport.ReadEntries<MtEntry>(entryBr, _header.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var fileStream = new MtBlowfishStream(new SubStream(input, entry.Offset, entry.CompSize), key);
                var name = entry.FileName + MtArcSupport.DetermineExtension(entry.ExtensionHash);

                result.Add(MtArc.CreateAfi(fileStream, name, entry, MtArcPlatform.LittleEndian));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            var key = GetCipherKey("imaguy_uyrag_igurustim_", "enokok_ikorodo_odohuran");

            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = MtArcSupport.DetermineFileOffset(ByteOrder.LittleEndian, _header.version, files.Count, entryOffset, false);

            // Prepare output stream
            output.SetLength(MtArc.GetArchiveSize(files, _header.version, ByteOrder.LittleEndian));

            // Write files
            var entries = new List<IMtEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<MtArchiveFile>())
            {
                var fileStream = file.GetFinalStream();
                Stream targetStream = new SubStream(output, filePosition, fileStream.Length);
                targetStream = new MtBlowfishStream(targetStream, key);

                fileStream.CopyTo(targetStream);

                file.Entry.Offset = filePosition;
                file.Entry.SetDecompressedSize((int)file.FileSize, MtArcPlatform.LittleEndian);
                file.Entry.CompSize = (int)fileStream.Length;
                
                entries.Add(file.Entry);

                filePosition += (int)fileStream.Length;
            }

            // Write entries
            Stream entryStream = new SubStream(output, entryOffset, output.Length - entryOffset);
            entryStream = new MtBlowfishStream(entryStream, key);
            using var entryBw = new BinaryWriterX(entryStream);

            MtArcSupport.WriteEntries(entries, entryBw);

            // Write header
            _header.entryCount = (short)files.Count;

            output.Position = 0;
            MtArcSupport.WriteHeader(_header, bw);
        }

        private byte[] GetCipherKey(string key1, string key2) => key1.Reverse().Select((c, i) => (byte)(c ^ key2[i] | i << 6)).ToArray();
    }
}
