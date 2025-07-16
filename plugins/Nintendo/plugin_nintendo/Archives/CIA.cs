using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Kryptography.Checksum;

namespace plugin_nintendo.Archives
{
    public class CIA
    {
        private const int HeaderSize_ = 0x2040;
        private const int ContentChunkRecordSize_ = 0x30;

        private CiaHeader _header;
        private CiaCertificateChain _certChain;
        private CiaTicket _ticket;
        private CiaTmd _tmd;
        private CiaMeta _meta;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadCiaHeader(br);
            br.SeekAlignment(0x40);

            // Read certificate chain
            _certChain = ReadCiaCertificateChain(br);
            br.SeekAlignment(0x40);

            // Read ticket
            _ticket = ReadCiaTicket(br);
            br.SeekAlignment(0x40);

            // Read TMD
            _tmd = ReadCiaTmd(br);
            br.SeekAlignment(0x40);

            // Declare NCCH partitions
            var result = new List<IArchiveFile>();

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
                ncchStream.Read(flags);
                ncchStream.Position = 0;

                result.Add(new CiaArchiveFile(new ArchiveFileInfo
                {
                    FilePath = GetPartitionName(flags[5], index),
                    FileData = ncchStream,
                    PluginIds = [Guid.Parse("7d0177a6-1cab-44b3-bf22-39f5548d6cac")]
                }, _tmd.contentChunkRecords[index]));

                index++;
            }

            // Read meta data
            br.BaseStream.Position = ncchPartitionOffset;
            if (_header.metaSize != 0)
            {
                _meta = ReadCiaMeta(br);
                br.SeekAlignment(0x40);
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var ciaAfis = files.Cast<CiaArchiveFile>().ToArray();
            var hash = new Sha256();

            // Update content chunks
            foreach (var ciaAfi in ciaAfis)
            {
                var ncchStream = ciaAfi.GetFileData().Result;

                ciaAfi.ContentChunkRecord.sha256 = hash.Compute(ncchStream);
                ciaAfi.ContentChunkRecord.contentSize = ncchStream.Length;
            }
            _tmd.contentChunkRecords = ciaAfis.Select(x => x.ContentChunkRecord).ToArray();

            // Write content chunks
            var contentChunkStream = new MemoryStream();
            using (var chunkBw = new BinaryWriterX(contentChunkStream, true))
                WriteCiaContentChunkRecords(_tmd.contentChunkRecords, chunkBw);

            // Update content info records
            foreach (var contentInfoRecord in _tmd.contentInfoRecords)
            {
                if (contentInfoRecord.contentChunkCount == 0)
                    continue;

                var offset = contentInfoRecord.contentChunkIndex * ContentChunkRecordSize_;
                var size = contentInfoRecord.contentChunkCount * ContentChunkRecordSize_;
                contentInfoRecord.sha256 = hash.Compute(new SubStream(contentChunkStream, offset, size));
            }

            // Write content info records
            var contentInfoStream = new MemoryStream();
            using (var infoBw = new BinaryWriterX(contentInfoStream, true))
                WriteCiaContentInfoRecords(_tmd.contentInfoRecords, infoBw);

            // Update content info hash
            contentInfoStream.Position = 0;
            _tmd.header.sha256 = hash.Compute(contentInfoStream);

            // --- Write CIA ---
            using var bw = new BinaryWriterX(output);
            var ciaOffset = bw.BaseStream.Position = HeaderSize_;

            // Write certificate chain
            WriteCiaCertificateChain(_certChain, bw);
            _header.certChainSize = (int)(bw.BaseStream.Length - ciaOffset);
            bw.WriteAlignment(0x40);
            ciaOffset = bw.BaseStream.Length;

            // Write ticket
            WriteCiaTicket(_ticket, bw);
            _header.ticketSize = (int)(bw.BaseStream.Length - ciaOffset);
            bw.WriteAlignment(0x40);
            ciaOffset = bw.BaseStream.Length;

            // Write TMD
            WriteCiaTmd(_tmd, bw);
            _header.tmdSize = (int)(bw.BaseStream.Length - ciaOffset);
            bw.WriteAlignment(0x40);
            ciaOffset = bw.BaseStream.Length;

            // Write content
            foreach (IArchiveFile ciaAfi in ciaAfis)
                ciaAfi.WriteFileData(bw.BaseStream, true);
            _header.contentSize = bw.BaseStream.Length - ciaOffset;
            bw.WriteAlignment(0x40);
            ciaOffset = bw.BaseStream.Length;

            // Write meta data
            if (_meta != null)
            {
                WriteCiaMeta(_meta, bw);
                _header.metaSize = (int)(bw.BaseStream.Length - ciaOffset);
                bw.WriteAlignment(0x40);
            }

            // Write header
            bw.BaseStream.Position = 0;
            WriteCiaHeader(_header, bw);
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

        #region Read

        private CiaHeader ReadCiaHeader(BinaryReaderX br)
        {
            return new CiaHeader
            {
                headerSize = br.ReadInt32(),
                type = br.ReadInt16(),
                version = br.ReadInt16(),
                certChainSize = br.ReadInt32(),
                ticketSize = br.ReadInt32(),
                tmdSize = br.ReadInt32(),
                metaSize = br.ReadInt32(),
                contentSize = br.ReadInt64(),
                contentIndex = br.ReadBytes(0x2000)
            };
        }

        private CiaCertificateChain ReadCiaCertificateChain(BinaryReaderX br)
        {
            return new CiaCertificateChain
            {
                ca = ReadCiaCertificate(br),
                tmdVerifier = ReadCiaCertificate(br),
                ticketVerifier = ReadCiaCertificate(br)
            };
        }

        private CiaCertificate ReadCiaCertificate(BinaryReaderX br)
        {
            var byteOrder = br.ByteOrder;
            br.ByteOrder = ByteOrder.BigEndian;

            var cert = new CiaCertificate
            {
                sigType = br.ReadInt32()
            };

            cert.signature = br.ReadBytes(CiaSupport.GetSignatureLength(cert.sigType));
            cert.signaturePadding = br.ReadBytes(CiaSupport.GetSignaturePadding(cert.sigType));

            cert.issuer = br.ReadString(0x40);
            cert.keyType = br.ReadInt32();
            cert.name = br.ReadString(0x40);

            cert.publicKey = br.ReadBytes(CiaSupport.GetPublicKeyLength(cert.keyType));
            cert.unk1 = br.ReadInt32();
            cert.publicKeyPadding = br.ReadBytes(CiaSupport.GetPublicKeyPadding(cert.keyType));

            br.ByteOrder = byteOrder;

            return cert;
        }

        private CiaTicket ReadCiaTicket(BinaryReaderX br)
        {
            var byteOrder = br.ByteOrder;
            br.ByteOrder = ByteOrder.BigEndian;

            var ticket = new CiaTicket
            {
                sigType = br.ReadInt32()
            };

            ticket.signature = br.ReadBytes(CiaSupport.GetSignatureLength(ticket.sigType));
            ticket.signaturePadding = br.ReadBytes(CiaSupport.GetSignaturePadding(ticket.sigType));

            ticket.ticketData = ReadCiaTicketData(br);

            br.ByteOrder = byteOrder;

            return ticket;
        }

        private CiaTicketData ReadCiaTicketData(BinaryReaderX br)
        {
            var byteOrder = br.ByteOrder;
            br.ByteOrder = ByteOrder.BigEndian;

            var ticketData = new CiaTicketData
            {
                issuer = br.ReadString(0x40),
                eccPublicKey = br.ReadBytes(0x3C),
                version = br.ReadByte(),
                caCrlVersion = br.ReadByte(),
                signerCrlVersion = br.ReadByte(),
                titleKey = br.ReadBytes(0x10),
                reserved1 = br.ReadByte(),
                ticketID = br.ReadUInt64(),
                consoleID = br.ReadUInt32(),
                titleID = br.ReadUInt64(),
                reserved2 = br.ReadInt16(),
                ticketTitleVersion = br.ReadInt16(),
                reserved3 = br.ReadUInt64(),
                licenseType = br.ReadByte(),
                keyYIndex = br.ReadByte(),
                reserved4 = br.ReadBytes(0x2A),
                eshopAccID = br.ReadUInt32(),
                reserved5 = br.ReadByte(),
                audit = br.ReadByte(),
                reserved6 = br.ReadBytes(0x42),
                limits = br.ReadBytes(0x40),
                contentIndex = br.ReadBytes(0xAC)
            };

            br.ByteOrder = byteOrder;

            return ticketData;
        }

        private CiaTmd ReadCiaTmd(BinaryReaderX br)
        {
            var byteOrder = br.ByteOrder;
            br.ByteOrder = ByteOrder.BigEndian;

            var tmd = new CiaTmd
            {
                sigType = br.ReadInt32()
            };

            tmd.signature = br.ReadBytes(CiaSupport.GetSignatureLength(tmd.sigType));
            tmd.signaturePadding = br.ReadBytes(CiaSupport.GetSignaturePadding(tmd.sigType));

            tmd.header = ReadCiaTmdHeader(br);
            tmd.contentInfoRecords = ReadCiaContentInfoRecords(br);
            tmd.contentChunkRecords = ReadCiaContentChunkRecords(br, tmd.header.contentCount);

            br.ByteOrder = byteOrder;

            return tmd;
        }

        private CiaTmdHeader ReadCiaTmdHeader(BinaryReaderX br)
        {
            var byteOrder = br.ByteOrder;
            br.ByteOrder = ByteOrder.BigEndian;

            var tmdHeader = new CiaTmdHeader
            {
                issuer = br.ReadString(0x40),
                version = br.ReadByte(),
                caCrlVersion = br.ReadByte(),
                signerCrlVersion = br.ReadByte(),
                reserved1 = br.ReadByte(),
                systemVersion = br.ReadInt64(),
                titleID = br.ReadUInt64(),
                titleType = br.ReadInt32(),
                groupID = br.ReadInt16(),
                saveDataSize = br.ReadInt32(),
                srlPrivateSaveDataSize = br.ReadInt32(),
                reserved2 = br.ReadInt32(),
                srlFlag = br.ReadByte(),
                reserved3 = br.ReadBytes(0x31),
                accessRights = br.ReadInt32(),
                titleVersion = br.ReadInt16(),
                contentCount = br.ReadInt16(),
                bootContent = br.ReadInt16(),
                padding = br.ReadInt16(),
                sha256 = br.ReadBytes(0x20)
            };

            br.ByteOrder = byteOrder;

            return tmdHeader;
        }

        private CiaContentInfoRecord[] ReadCiaContentInfoRecords(BinaryReaderX br)
        {
            var result = new CiaContentInfoRecord[0x40];

            for (var i = 0; i < 0x40; i++)
                result[i] = ReadCiaContentInfoRecord(br);

            return result;
        }

        private CiaContentInfoRecord ReadCiaContentInfoRecord(BinaryReaderX br)
        {
            var byteOrder = br.ByteOrder;
            br.ByteOrder = ByteOrder.BigEndian;

            var infoRecord = new CiaContentInfoRecord
            {
                contentChunkIndex = br.ReadInt16(),
                contentChunkCount = br.ReadInt16(),
                sha256 = br.ReadBytes(0x20)
            };

            br.ByteOrder = byteOrder;

            return infoRecord;
        }

        private CiaContentChunkRecord[] ReadCiaContentChunkRecords(BinaryReaderX br, int count)
        {
            var result = new CiaContentChunkRecord[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadCiaContentChunkRecord(br);

            return result;
        }

        private CiaContentChunkRecord ReadCiaContentChunkRecord(BinaryReaderX br)
        {
            var byteOrder = br.ByteOrder;
            br.ByteOrder = ByteOrder.BigEndian;

            var chunkRecord = new CiaContentChunkRecord
            {
                contentID = br.ReadInt32(),
                contentIndex = br.ReadInt16(),
                contentType = br.ReadInt16(),
                contentSize = br.ReadInt64(),
                sha256 = br.ReadBytes(0x20)
            };

            br.ByteOrder = byteOrder;

            return chunkRecord;
        }

        private CiaMeta ReadCiaMeta(BinaryReaderX br)
        {
            return new CiaMeta
            {
                titleIDDependency = br.ReadBytes(0x180),
                reserved1 = br.ReadBytes(0x180),
                coreVersion = br.ReadInt32(),
                reserved2 = br.ReadBytes(0xFC),
                iconData = br.ReadBytes(0x36C0)
            };
        }

        #endregion

        #region Write

        private void WriteCiaHeader(CiaHeader header, BinaryWriterX bw)
        {
            bw.Write(header.headerSize);
            bw.Write(header.type);
            bw.Write(header.version);
            bw.Write(header.certChainSize);
            bw.Write(header.ticketSize);
            bw.Write(header.tmdSize);
            bw.Write(header.metaSize);
            bw.Write(header.contentSize);
            bw.Write(header.contentIndex);
        }

        private void WriteCiaCertificateChain(CiaCertificateChain certChain, BinaryWriterX bw)
        {
            WriteCiaCertificate(certChain.ca, bw);
            WriteCiaCertificate(certChain.tmdVerifier, bw);
            WriteCiaCertificate(certChain.ticketVerifier, bw);
        }

        private void WriteCiaCertificate(CiaCertificate cert, BinaryWriterX bw)
        {
            var byteOrder = bw.ByteOrder;
            bw.ByteOrder = ByteOrder.BigEndian;

            bw.Write(cert.sigType);
            bw.Write(cert.signature);
            bw.Write(cert.signaturePadding);
            bw.WriteString(cert.issuer, writeNullTerminator: false);
            bw.Write(cert.keyType);
            bw.WriteString(cert.name, writeNullTerminator: false);
            bw.Write(cert.publicKey);
            bw.Write(cert.unk1);
            bw.Write(cert.publicKeyPadding);

            bw.ByteOrder = byteOrder;
        }

        private void WriteCiaTicket(CiaTicket ticket, BinaryWriterX bw)
        {
            var byteOrder = bw.ByteOrder;
            bw.ByteOrder = ByteOrder.BigEndian;

            bw.Write(ticket.sigType);
            bw.Write(ticket.signature);
            bw.Write(ticket.signaturePadding);

            WriteCiaTicketData(ticket.ticketData, bw);

            bw.ByteOrder = byteOrder;
        }

        private void WriteCiaTicketData(CiaTicketData ticketData, BinaryWriterX bw)
        {
            var byteOrder = bw.ByteOrder;
            bw.ByteOrder = ByteOrder.BigEndian;

            bw.WriteString(ticketData.issuer, writeNullTerminator: false);
            bw.Write(ticketData.eccPublicKey);
            bw.Write(ticketData.version);
            bw.Write(ticketData.caCrlVersion);
            bw.Write(ticketData.signerCrlVersion);
            bw.Write(ticketData.titleKey);
            bw.Write(ticketData.reserved1);
            bw.Write(ticketData.ticketID);
            bw.Write(ticketData.consoleID);
            bw.Write(ticketData.titleID);
            bw.Write(ticketData.reserved2);
            bw.Write(ticketData.ticketTitleVersion);
            bw.Write(ticketData.reserved3);
            bw.Write(ticketData.licenseType);
            bw.Write(ticketData.keyYIndex);
            bw.Write(ticketData.reserved4);
            bw.Write(ticketData.eshopAccID);
            bw.Write(ticketData.reserved5);
            bw.Write(ticketData.audit);
            bw.Write(ticketData.reserved6);
            bw.Write(ticketData.limits);
            bw.Write(ticketData.contentIndex);

            bw.ByteOrder = byteOrder;
        }

        private void WriteCiaTmd(CiaTmd tmd, BinaryWriterX bw)
        {
            var byteOrder = bw.ByteOrder;
            bw.ByteOrder = ByteOrder.BigEndian;

            bw.Write(tmd.sigType);
            bw.Write(tmd.signature);
            bw.Write(tmd.signaturePadding);

            WriteCiaTmdHeader(tmd.header, bw);
            WriteCiaContentInfoRecords(tmd.contentInfoRecords, bw);
            WriteCiaContentChunkRecords(tmd.contentChunkRecords, bw);

            bw.ByteOrder = byteOrder;
        }

        private void WriteCiaTmdHeader(CiaTmdHeader tmdHeader, BinaryWriterX bw)
        {
            var byteOrder = bw.ByteOrder;
            bw.ByteOrder = ByteOrder.BigEndian;

            bw.WriteString(tmdHeader.issuer, writeNullTerminator: false);
            bw.Write(tmdHeader.version);
            bw.Write(tmdHeader.caCrlVersion);
            bw.Write(tmdHeader.signerCrlVersion);
            bw.Write(tmdHeader.reserved1);
            bw.Write(tmdHeader.systemVersion);
            bw.Write(tmdHeader.titleID);
            bw.Write(tmdHeader.titleType);
            bw.Write(tmdHeader.groupID);
            bw.Write(tmdHeader.saveDataSize);
            bw.Write(tmdHeader.srlPrivateSaveDataSize);
            bw.Write(tmdHeader.reserved2);
            bw.Write(tmdHeader.srlFlag);
            bw.Write(tmdHeader.reserved3);
            bw.Write(tmdHeader.accessRights);
            bw.Write(tmdHeader.titleVersion);
            bw.Write(tmdHeader.contentCount);
            bw.Write(tmdHeader.bootContent);
            bw.Write(tmdHeader.padding);
            bw.Write(tmdHeader.sha256);

            bw.ByteOrder = byteOrder;
        }

        private void WriteCiaContentInfoRecords(CiaContentInfoRecord[] records, BinaryWriterX bw)
        {
            for (var i = 0; i < 0x40; i++)
                WriteCiaContentInfoRecord(records[i], bw);
        }

        private void WriteCiaContentInfoRecord(CiaContentInfoRecord record, BinaryWriterX bw)
        {
            var byteOrder = bw.ByteOrder;
            bw.ByteOrder = ByteOrder.BigEndian;

            bw.Write(record.contentChunkIndex);
            bw.Write(record.contentChunkCount);
            bw.Write(record.sha256);

            bw.ByteOrder = byteOrder;
        }

        private void WriteCiaContentChunkRecords(CiaContentChunkRecord[] records, BinaryWriterX bw)
        {
            foreach (CiaContentChunkRecord record in records)
                WriteCiaContentChunkRecord(record, bw);
        }

        private void WriteCiaContentChunkRecord(CiaContentChunkRecord record, BinaryWriterX bw)
        {
            var byteOrder = bw.ByteOrder;
            bw.ByteOrder = ByteOrder.BigEndian;

            bw.Write(record.contentID);
            bw.Write(record.contentIndex);
            bw.Write(record.contentType);
            bw.Write(record.contentSize);
            bw.Write(record.sha256);

            bw.ByteOrder = byteOrder;
        }

        private void WriteCiaMeta(CiaMeta meta, BinaryWriterX bw)
        {
            bw.Write(meta.titleIDDependency);
            bw.Write(meta.reserved1);
            bw.Write(meta.coreVersion);
            bw.Write(meta.reserved2);
            bw.Write(meta.iconData);
        }

        #endregion
    }
}
