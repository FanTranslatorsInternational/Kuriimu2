using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    class NARC
    {
        private static readonly int NarcHeaderSize = Tools.MeasureType(typeof(NarcHeader));
        private static readonly int FatHeaderSize = Tools.MeasureType(typeof(NarcFatHeader));
        private static readonly int FatEntrySize = Tools.MeasureType(typeof(FatEntry));

        private bool _hasNames;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Determine byte order
            br.BaseStream.Position = 4;
            var byteOrder = (ByteOrder)br.ReadUInt16();
            br.ByteOrder = byteOrder;

            // Read header
            br.BaseStream.Position = 0;
            var header = br.ReadType<NarcHeader>();

            // Read file entries
            var fatHeader = br.ReadType<NarcFatHeader>();
            var entries = br.ReadMultiple<FatEntry>(fatHeader.fileCount);

            // Read FNT
            var fntOffset = (int)br.BaseStream.Position;
            var fntHeader = br.ReadType<NarcFntHeader>();

            var gmifOffset = fntOffset + fntHeader.chunkSize;

            _hasNames = br.ReadInt32() >= 8;
            if (_hasNames)
                return NdsSupport.ReadFnt(br, fntOffset, entries).ToList();

            return entries.Select((x, i) => NdsSupport.CreateAfi(br.BaseStream, x.offset + gmifOffset + 8, x.Length, $"{i:00000000}.bin", i)).ToArray();
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fatOffset = NarcHeaderSize;
            var fntOffset = fatOffset + FatHeaderSize + files.Count * FatEntrySize;

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
                NdsSupport.WriteFnt(bw, fntOffset + 8, files);
                fntSize = (int)(bw.BaseStream.Position - fntOffset);
            }

            output.Position = fntOffset;
            bw.WriteType(new NarcFntHeader
            {
                chunkSize = fntSize
            });

            // Write GMIF
            var fatEntries = new List<FatEntry>();

            var gmifOffset = fntOffset + fntSize;
            output.Position = gmifOffset + 8;
            foreach (var file in files.Cast<FileIdArchiveFileInfo>().OrderBy(x => x.FileId))
            {
                var filePosition = output.Position;
                var writtenSize = file.SaveFileData(output);

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
            bw.WriteType(new NarcFatHeader
            {
                chunkSize = FatHeaderSize + files.Count * FatEntrySize,
                fileCount = (short)files.Count
            });
            bw.WriteMultiple(fatEntries);

            // Write header
            output.Position = 0;
            bw.WriteType(new NarcHeader
            {
                fileSize = (int)output.Length
            });
        }
    }
}
