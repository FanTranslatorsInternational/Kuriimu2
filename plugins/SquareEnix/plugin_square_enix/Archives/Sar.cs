using System;
using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using plugin_square_enix.Compression;
using System.Linq;
using System.Text;

namespace plugin_square_enix.Archives
{
    class Sar
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(SarContainerHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(SarEntry));

        private SarContainerHeader _header;

        public IList<IArchiveFileInfo> Load(Stream dataStream, Stream matStream)
        {
            using var br = new BinaryReaderX(dataStream, true);
            using var matBr = new BinaryReaderX(matStream);

            // Read entries
            var entries = matBr.ReadMultiple<SarEntry>((int)(matStream.Length / EntrySize));

            // Read header
            _header = br.ReadType<SarContainerHeader>();

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                dataStream.Position = entry.offset;

                // Read compression header
                var compHeader = br.ReadType<SarContainerHeader>();
                if (compHeader.magic != "cmp ")
                {
                    result.Add(new SarArchiveFileInfo(new SubStream(dataStream, entry.offset, entry.size), $"{i:00000000}.bin"));
                    continue;
                }

                compHeader = br.ReadType<SarContainerHeader>();
                if (compHeader.magic != "lz7 ")
                    throw new InvalidOperationException($"Unknown compression container detected for file at index {i}.");

                var fileStream = new SubStream(dataStream, entry.offset + 0x18, SarSupport.GetCompressedSize(dataStream, entry.offset + 0x18, compHeader.data2));
                var name = $"{i:00000000}.bin";

                var compMethod = NintendoCompressor.PeekCompressionMethod(fileStream);
                result.Add(new SarArchiveFileInfo(fileStream, name, NintendoCompressor.GetConfiguration(compMethod), NintendoCompressor.PeekDecompressedSize(fileStream)));
            }

            return result;
        }

        public void Save(Stream dataStream, Stream matStream, IList<IArchiveFileInfo> files)
        {
            long endPos;

            using var bw = new BinaryWriterX(dataStream);
            using var matBw = new BinaryWriterX(matStream);

            // Calculate offsets
            var mbrOffset = HeaderSize;
            var dataOffset = mbrOffset + HeaderSize;

            // Write files
            var entries = new List<SarEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<SarArchiveFileInfo>())
            {
                // Write file data1
                dataStream.Position = dataPosition;
                if (file.UsesCompression)
                    dataStream.Position += HeaderSize * 2;

                var streamToWrite = file.GetFinalStream();
                streamToWrite.CopyTo(dataStream);
                var alignedSize = (streamToWrite.Length + 3) & ~3;

                // Write compression headers
                if (file.UsesCompression)
                {
                    endPos = dataStream.Position;
                    dataStream.Position = dataPosition;

                    bw.WriteType(new SarContainerHeader { magic = "cmp ", data1 = 0x00010002, data2 = (int)(alignedSize + HeaderSize * 2 + 8) });
                    // Divide bit count by 8; this may omit the remainder and is intended in the size calculation
                    // This recreates a buggy behaviour by the developers, who missed to account for the remainder properly, which can lead to the compressed size being off by 1
                    bw.WriteType(new SarContainerHeader { magic = "lz7 ", data1 = (int)(alignedSize + HeaderSize + 4), data2 = SarSupport.CalculateBits(streamToWrite) / 8 });

                    dataStream.Position = endPos;
                    bw.WriteString("~lz7", Encoding.ASCII, false, false);
                    bw.WriteAlignment(4);
                    bw.WriteString("~cmp", Encoding.ASCII, false, false);
                }

                // Add entry
                entries.Add(new SarEntry { offset = dataPosition - dataOffset, size = (int)(dataStream.Position - dataPosition) });

                dataPosition = (int)dataStream.Position;
            }

            // Write mbr header
            endPos = dataStream.Position;

            dataStream.Position = mbrOffset;
            bw.WriteType(new SarContainerHeader { magic = "mbr ", data1 = (int)(dataStream.Length - HeaderSize + 4), data2 = files.Count });

            dataStream.Position = endPos;
            bw.WriteString("~mbr", Encoding.ASCII, false, false);

            // Write entries
            var mifOffset = dataStream.Position;

            dataStream.Position += HeaderSize;
            bw.WriteMultiple(entries);

            // Write mif header
            endPos = dataStream.Position;

            dataStream.Position = mifOffset;
            bw.WriteType(new SarContainerHeader { magic = "mif ", data1 = (int)(dataStream.Length - mifOffset + 4), data2 = files.Count });

            dataStream.Position = endPos;
            bw.WriteString("~mif", Encoding.ASCII, false, false);

            // Write sar header
            bw.WriteString("~sar", Encoding.ASCII, false, false);

            dataStream.Position = 0;
            bw.WriteType(new SarContainerHeader { magic = "sar ", data1 = _header.data1, data2 = (int)dataStream.Length });

            // Write mat content
            foreach (var entry in entries)
                entry.offset += dataOffset; // Offsets in .mat are absolute to the .sar
            matBw.WriteMultiple(entries);
        }
    }
}
