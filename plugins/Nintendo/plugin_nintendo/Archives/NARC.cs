using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    class Narc
    {
        private const int NarcHeaderSize_ = 0x10;
        private const int FatHeaderSize_ = 0xC;
        private const int FatEntrySize_ = 0x8;

        private bool _hasNames;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true);

            // Determine byte order
            br.BaseStream.Position = 4;
            br.ByteOrder = (ByteOrder)br.ReadUInt16();

            // Read header
            br.BaseStream.Position = 0;
            var header = typeReader.Read<NarcHeader>(br);

            // Read file entries
            var fatHeader = typeReader.Read<NarcFatHeader>(br);
            var entries = typeReader.ReadMany<FatEntry>(br, fatHeader.fileCount);

            // Read FNT
            var fntOffset = (int)br.BaseStream.Position;
            var fntHeader = typeReader.Read<NarcFntHeader>(br);

            var gmifOffset = fntOffset + fntHeader.chunkSize;

            _hasNames = br.ReadInt32() >= 8;
            if (_hasNames)
                return NdsSupport.ReadFnt(typeReader, br, fntOffset + 8, gmifOffset + 8, entries).ToList();

            return entries.Select((x, i) => NdsSupport.CreateAfi(br.BaseStream, x.offset + gmifOffset + 8, x.Length, $"{i:00000000}.bin", i)).ToList();
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fatOffset = NarcHeaderSize_;
            var fntOffset = fatOffset + FatHeaderSize_ + files.Count * FatEntrySize_;

            // Write FNT
            int fntSize;
            if (!_hasNames)
            {
                output.Position = fntOffset + 8;
                bw.Write(4);
                bw.Write(0x10000);
                fntSize = 0x10;
            }
            else
            {
                NdsSupport.WriteFnt(typeWriter, bw, fntOffset + 8, files);
                fntSize = (int)(bw.BaseStream.Position - fntOffset);
            }

            output.Position = fntOffset;
            typeWriter.Write(new NarcFntHeader
            {
                chunkSize = fntSize
            }, bw);

            // Write GMIF
            var fatEntries = new List<FatEntry>();

            var gmifOffset = fntOffset + fntSize;
            output.Position = gmifOffset + 8;
            foreach (var file in files.Cast<FileIdArchiveFile>().OrderBy(x => x.FileId))
            {
                var filePosition = output.Position;
                var writtenSize = file.WriteFileData(output, true);

                fatEntries.Add(new FatEntry
                {
                    offset = (int)filePosition - gmifOffset - 8,
                    endOffset = (int)(filePosition - gmifOffset - 8 + writtenSize)
                });
            }

            output.Position = gmifOffset;
            bw.WriteString("GMIF", Encoding.ASCII, false, false);
            bw.Write((int)(output.Length - gmifOffset));

            // Write FAT
            output.Position = fatOffset;
            typeWriter.Write(new NarcFatHeader
            {
                chunkSize = FatHeaderSize_ + files.Count * FatEntrySize_,
                fileCount = (short)files.Count
            }, bw);
            typeWriter.WriteMany(fatEntries, bw);

            // Write header
            output.Position = 0;
            typeWriter.Write(new NarcHeader
            {
                fileSize = (int)output.Length
            }, bw);
        }
    }
}
