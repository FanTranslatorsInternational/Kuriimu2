using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_mt_framework.Archives
{
    class Arcc
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(MtHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(MtEntry));

        private MtHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<MtHeader>();

            // Read entries
            var key = GetCipherKey("imaguy_uyrag_igurustim_", "enokok_ikorodo_odohuran");
            var entryStream = new MtBlowfishStream(new SubStream(input, HeaderSize, _header.entryCount * EntrySize), key);

            using var entryBr = new BinaryReaderX(entryStream);
            var entries = entryBr.ReadMultiple<MtEntry>(_header.entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileStream = new MtBlowfishStream(new SubStream(input, entry.Offset, entry.CompSize), key);
                var name = entry.FileName + MtArcSupport.DetermineExtension(entry.ExtensionHash);

                result.Add(MtArc.CreateAfi(fileStream, name, entry, MtArcPlatform.LittleEndian));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var key = GetCipherKey("imaguy_uyrag_igurustim_", "enokok_ikorodo_odohuran");

            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = MtArcSupport.DetermineFileOffset(ByteOrder.LittleEndian, _header.version, files.Count, entryOffset);

            // Prepare output stream
            output.SetLength(MtArc.GetArchiveSize(files, _header.version, ByteOrder.LittleEndian));

            // Write files
            var entries = new List<IMtEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<MtArchiveFileInfo>())
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
            entryStream=new MtBlowfishStream(entryStream,key);
            using var entryBw = new BinaryWriterX(entryStream);

            entryBw.WriteMultiple(entries);

            // Write header
            _header.entryCount = (short)files.Count;

            output.Position = 0;
            bw.WriteType(_header);
        }

        private byte[] GetCipherKey(string key1, string key2) => key1.Reverse().Select((c, i) => (byte)(c ^ key2[i] | i << 6)).ToArray();
    }
}
