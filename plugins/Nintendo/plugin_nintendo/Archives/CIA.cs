using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kryptography.Hash;

namespace plugin_nintendo.Archives
{
    public class CIA
    {
        private const int Alignment_ = 0x40;

        private static int _headerSize = 0x2040;
        private static int _contentChunkRecordSize = Tools.MeasureType(typeof(CiaContentChunkRecord));

        private CiaHeader _header;
        private CiaCertificateChain _certChain;
        private CiaTicket _ticket;
        private CiaTmd _tmd;
        private CiaMeta _meta;

        public IList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<CiaHeader>();
            br.SeekAlignment(0x40);

            // Read certificate chain
            _certChain = br.ReadType<CiaCertificateChain>();
            br.SeekAlignment(0x40);

            // Read ticket
            _ticket = br.ReadType<CiaTicket>();
            br.SeekAlignment(0x40);

            // Read TMD
            _tmd = br.ReadType<CiaTmd>();
            br.SeekAlignment(0x40);

            // Declare NCCH partitions
            var result = new List<ArchiveFileInfo>();

            var ncchStreams = new List<SubStream>();
            var ncchPartitionOffset = br.BaseStream.Position;
            foreach (var contentChunkRecord in _tmd.contentChunkRecords)
            {
                ncchStreams.Add(new SubStream(br.BaseStream, ncchPartitionOffset, contentChunkRecord.contentSize));
                ncchPartitionOffset += contentChunkRecord.contentSize;
            }

            var index = 0;
            foreach (var ncchStream in ncchStreams)
            {
                ncchStream.Position = 0x188;
                var flags = new byte[8];
                ncchStream.Read(flags, 0, 8);
                ncchStream.Position = 0;

                result.Add(new CiaArchiveFileInfo(ncchStream, GetPartitionName(flags[5], index), _tmd.contentChunkRecords[index])
                {
                    // Add NCCH plugin
                    PluginIds = new[] { Guid.Parse("7d0177a6-1cab-44b3-bf22-39f5548d6cac") }
                });

                index++;
            }

            // Read meta data
            br.BaseStream.Position = ncchPartitionOffset;
            if (_header.metaSize != 0)
            {
                _meta = br.ReadType<CiaMeta>();
                br.SeekAlignment(0x40);
            }

            return result;
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files)
        {
            var ciaAfis = files.Cast<CiaArchiveFileInfo>().ToArray();
            var sha = new Sha256();

            // Update content chunks
            foreach (var ciaAfi in ciaAfis)
            {
                var ncchStream = ciaAfi.GetFileData().Result;

                ciaAfi.ContentChunkRecord.sha256 = sha.Compute(ncchStream);
                ciaAfi.ContentChunkRecord.contentSize = ncchStream.Length;
            }
            _tmd.contentChunkRecords = ciaAfis.Select(x => x.ContentChunkRecord).ToArray();

            // Write content chunks
            var contentChunkStream = new MemoryStream();
            using (var chunkBw = new BinaryWriterX(contentChunkStream, true))
                chunkBw.WriteMultiple(_tmd.contentChunkRecords);

            // Update content info records
            foreach (var contentInfoRecord in _tmd.contentInfoRecords)
            {
                if (contentInfoRecord.contentChunkCount == 0)
                    continue;

                var offset = contentInfoRecord.contentChunkIndex * _contentChunkRecordSize;
                var size = contentInfoRecord.contentChunkCount * _contentChunkRecordSize;
                contentInfoRecord.sha256 = sha.Compute(new SubStream(contentChunkStream, offset, size));
            }

            // Write content info records
            var contentInfoStream = new MemoryStream();
            using (var infoBw = new BinaryWriterX(contentInfoStream, true))
                infoBw.WriteMultiple(_tmd.contentInfoRecords);

            // Update content info hash
            contentInfoStream.Position = 0;
            _tmd.header.sha256 = sha.Compute(contentInfoStream);

            // --- Write CIA ---
            using var bw = new BinaryWriterX(output);
            var ciaOffset = bw.BaseStream.Position = _headerSize;

            // Write certificate chain
            bw.WriteType(_certChain);
            _header.certChainSize = (int)(bw.BaseStream.Length - ciaOffset);
            bw.WriteAlignment(0x40);
            ciaOffset = bw.BaseStream.Length;

            // Write ticket
            bw.WriteType(_ticket);
            _header.ticketSize = (int)(bw.BaseStream.Length - ciaOffset);
            bw.WriteAlignment(0x40);
            ciaOffset = bw.BaseStream.Length;

            // Write TMD
            bw.WriteType(_tmd);
            _header.tmdSize = (int)(bw.BaseStream.Length - ciaOffset);
            bw.WriteAlignment(0x40);
            ciaOffset = bw.BaseStream.Length;

            // Write content
            foreach (var ciaAfi in ciaAfis)
                ciaAfi.SaveFileData(bw.BaseStream);
            _header.contentSize = bw.BaseStream.Length - ciaOffset;
            bw.WriteAlignment(0x40);
            ciaOffset = bw.BaseStream.Length;

            // Write meta data
            if (_meta != null)
            {
                bw.WriteType(_meta);
                _header.metaSize = (int)(bw.BaseStream.Length - ciaOffset);
                bw.WriteAlignment(0x40);
            }

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(_header);
        }

        private string GetPartitionName(byte typeFlag, int index)
        {
            var ext = (typeFlag & 0x1) == 1 && ((typeFlag >> 1) & 0x1) == 1 ? ".cxi" : ".cfa";

            var fileName = "";
            if ((typeFlag & 0x1) == 1 && ((typeFlag >> 1) & 0x1) == 1)
                fileName = "GameData";
            else if ((typeFlag & 0x1) == 1 && ((typeFlag >> 2) & 0x1) == 1 && ((typeFlag >> 3) & 0x1) == 1)
                fileName = "DownloadPlay";
            else if ((typeFlag & 0x1) == 1 && ((typeFlag >> 2) & 0x1) == 1)
                fileName = "3DSUpdate";
            else if ((typeFlag & 0x1) == 1 && ((typeFlag >> 3) & 0x1) == 1)
                fileName = "Manual";
            else if ((typeFlag & 0x1) == 1 && ((typeFlag >> 4) & 0x1) == 1)
                fileName = "Trial";
            else if (typeFlag == 1)
                fileName = $"Data{index:000}";

            return fileName + ext;
        }
    }
}
