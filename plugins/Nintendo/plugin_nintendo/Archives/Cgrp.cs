using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_nintendo.Archives
{
    // Game: Unknown
    class Cgrp
    {
        private static int _headerSize = 0x14;
        private static int _headerPartitionSize = Tools.MeasureType(typeof(CgrpHeaderPartition));
        private static int _partitionHeaderSize = Tools.MeasureType(typeof(CgrpPartitionHeader));
        private static int _partitionEntrySize = Tools.MeasureType(typeof(CgrpPartitionEntry));
        private static int _fileEntrySize = Tools.MeasureType(typeof(CgrpFileEntry));
        private static int _extendedInfoSize = Tools.MeasureType(typeof(CgrpExtendedInfoEntry));

        private List<CgrpExtendedInfoEntry> _extendedInfo;

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<CgrpHeader>();

            // Read info section
            var infoPartitionEntry = header.partitions.First(x => x.partitionId == 0x7800);
            br.BaseStream.Position = infoPartitionEntry.partitionOffset;

            var infoPartition = br.ReadType<CgrpPartition>();
            var fileEntries = new List<CgrpFileEntry>();
            foreach (var entry in infoPartition.partitionEntries.Where(x => x.valueType == 0x7900))
            {
                br.BaseStream.Position = infoPartitionEntry.partitionOffset + _partitionHeaderSize + entry.value;
                fileEntries.Add(br.ReadType<CgrpFileEntry>());
            }

            // Read extended info
            _extendedInfo = null;

            var infxPartitionEntry = header.partitions.FirstOrDefault(x => x.partitionId == 0x7802);
            if (infxPartitionEntry != null)
            {
                br.BaseStream.Position = infxPartitionEntry.partitionOffset;

                var infxPartition = br.ReadType<CgrpPartition>();
                _extendedInfo = new List<CgrpExtendedInfoEntry>();
                foreach (var entry in infxPartition.partitionEntries.Where(x => x.valueType == 0x7901))
                {
                    br.BaseStream.Position = infxPartitionEntry.partitionOffset + _partitionHeaderSize + entry.value;
                    _extendedInfo.Add(br.ReadType<CgrpExtendedInfoEntry>());
                }
            }

            // Add files
            var dataPartition = header.partitions.First(x => x.partitionId == 0x7801);
            var result = new List<ArchiveFileInfo>();
            var fileId = 0;
            foreach (var entry in fileEntries)
            {
                var fileStream = new SubStream(input, dataPartition.partitionOffset + _partitionHeaderSize + entry.dataOffset, entry.dataSize);
                var extension = CgrpSupport.DetermineExtension(fileStream);
                result.Add(new CgrpArchiveFileInfo(fileStream, $"{fileId++:00000000}{extension}", entry));
            }

            return result;
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
            var castedFiles = files.Cast<CgrpArchiveFileInfo>().ToList();
            using var bw = new BinaryWriterX(output);

            var partitionCount = 2 + (_extendedInfo != null ? 1 : 0);
            var fileInfoPartitionPosition = (_headerSize + partitionCount * _headerPartitionSize + 0x1F) & ~0x1F;
            var fileDataPosition = (fileInfoPartitionPosition + _partitionHeaderSize + 4 +
                                   _partitionEntrySize * files.Count +
                                   _fileEntrySize * files.Count + 0x1F) & ~0x1F;

            var header = new CgrpHeader
            {
                headerSize = (short)fileInfoPartitionPosition,
                partitions = new CgrpHeaderPartition[partitionCount],
                partitionCount = partitionCount
            };

            // Write files
            var dataOffset = (fileDataPosition + _partitionHeaderSize + 0x1F) & ~0x1F;
            var fileOffset = dataOffset - fileDataPosition - _partitionHeaderSize;

            bw.BaseStream.Position = dataOffset;
            foreach (var file in castedFiles)
            {
                var writtenSize = file.SaveFileData(bw.BaseStream, null);
                bw.WriteAlignment(0x20);

                file.Entry.dataOffset = fileOffset;
                file.Entry.dataSize = (int)writtenSize;

                fileOffset = (int)bw.BaseStream.Position - fileDataPosition - _partitionHeaderSize;
            }

            bw.BaseStream.Position = fileDataPosition;
            bw.WriteString("FILE", Encoding.ASCII, false, false);
            bw.Write((int)bw.BaseStream.Length - fileDataPosition);

            header.partitions[1] = new CgrpHeaderPartition
            {
                partitionId = 0x7801,
                partitionOffset = fileDataPosition,
                partitionSize = (int)bw.BaseStream.Length - fileDataPosition
            };

            // Write extended info
            var extendedInfoPartitionPosition = (int)bw.BaseStream.Length;
            if (_extendedInfo != null)
            {
                bw.BaseStream.Position = extendedInfoPartitionPosition;

                bw.WriteString("INFX", Encoding.ASCII, false,false);
                bw.Write(_partitionHeaderSize + 4 + _extendedInfo.Count * (_partitionEntrySize + _extendedInfoSize));
                bw.Write(_extendedInfo.Count);

                var extendedInfoOffset = 4 + _extendedInfo.Count * _partitionEntrySize;
                for (var i = 0; i < _extendedInfo.Count; i++)
                {
                    bw.Write(0x7901);
                    bw.Write(extendedInfoOffset);

                    extendedInfoOffset += _extendedInfoSize;
                }

                bw.WriteMultiple(_extendedInfo);

                header.partitions[2] = new CgrpHeaderPartition
                {
                    partitionId = 0x7802,
                    partitionOffset = extendedInfoPartitionPosition,
                    partitionSize = (int)bw.BaseStream.Length - extendedInfoPartitionPosition
                };
            }

            // Write file infos
            bw.BaseStream.Position = fileInfoPartitionPosition;

            bw.WriteString("INFO", Encoding.ASCII, false, false);
            bw.Write((_partitionHeaderSize + 4 + files.Count * (_partitionEntrySize + _fileEntrySize) + 0x1F) & ~0x1F);
            bw.Write(files.Count);

            var fileInfoOffset = 4 + files.Count * _partitionEntrySize;
            for (var i = 0; i < files.Count; i++)
            {
                bw.Write(0x7900);
                bw.Write(fileInfoOffset);

                fileInfoOffset += _fileEntrySize;
            }

            bw.WriteMultiple(castedFiles.Select(x => x.Entry));
            bw.WriteAlignment(0x20);

            header.partitions[0] = new CgrpHeaderPartition
            {
                partitionId = 0x7800,
                partitionOffset = fileInfoPartitionPosition,
                partitionSize = (int)bw.BaseStream.Position - fileInfoPartitionPosition
            };

            // Write header
            header.fileSize = (uint)bw.BaseStream.Length;

            bw.BaseStream.Position = 0;
            bw.WriteType(header);
        }
    }
}
