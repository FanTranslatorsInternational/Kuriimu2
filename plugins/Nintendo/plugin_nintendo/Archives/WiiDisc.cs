using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.Plugin.File.Archive;
using Kryptography.Encryption.Nintendo.Wii;
using System.Net.Sockets;

namespace plugin_nintendo.Archives
{
    // TODO: Make partition reading its own plugin?
    class WiiDisc
    {
        public List<IArchiveFile> Load(Stream input)
        {
            var wiiDiscStream = new WiiDiscStream(input);

            using var br = new BinaryReaderX(wiiDiscStream, ByteOrder.BigEndian);

            // Read disc header
            _ = ReadDiscHeader(br);

            // Read partition infos
            br.BaseStream.Position = 0x40000;
            var partitionInformation = ReadPartitionInfo(br);

            // Read partitions
            br.BaseStream.Position = partitionInformation.partitionOffset1 << 2;
            var partitions = ReadPartitionEntries(br, partitionInformation.partitionCount1);

            // Read region settings
            br.BaseStream.Position = 0x4E000;
            _ = ReadSettings(br);

            // Read magic word
            br.BaseStream.Position = 0x4FFFC;
            var magic = br.ReadUInt32();
            if (magic != 0xC3F81A8E)
                throw new InvalidOperationException("Invalid Wii disc magic word.");

            // Read data partitions
            var result = new List<IArchiveFile>();
            foreach (var partition in partitions.Where(x => x.type == 0))
            {
                br.BaseStream.Position = partition.offset << 2;
                var partitionHeader = ReadPartitionHeader(br);

                var partitionStream = new SubStream(wiiDiscStream, (partition.offset << 2) + ((long)partitionHeader.dataOffset << 2), (long)partitionHeader.dataSize << 2);
                var partitionDataStream = new WiiDiscPartitionDataStream(partitionStream);

                using (var partitionBr = new BinaryReaderX(partitionDataStream, true, ByteOrder.BigEndian))
                {
                    // Read partition data header
                    _ = ReadDiscHeader(partitionBr);

                    // Read file system offset
                    partitionBr.BaseStream.Position = 0x424;
                    var fileSystemOffset = partitionBr.ReadInt32() << 2;
                    var fileSystemSize = partitionBr.ReadInt32() << 2;

                    // Parse file system
                    var fileSystem = new WiiDiscU8FileSystem("DATA");
                    result.AddRange(fileSystem.Parse(partitionDataStream, fileSystemOffset, fileSystemSize, fileSystemOffset));
                }
            }

            return result;
        }

        private WiiDiscHeader ReadDiscHeader(BinaryReaderX reader)
        {
            return new WiiDiscHeader
            {
                wiiDiscId = reader.ReadByte(),
                gameCode = reader.ReadInt16(),
                regionCode = reader.ReadByte(),
                makerCode = reader.ReadInt16(),
                discNumber = reader.ReadByte(),
                discVersion = reader.ReadByte(),
                enableAudioStreaming = reader.ReadBoolean(),
                streamBufferSize = reader.ReadByte(),
                zero0 = reader.ReadBytes(0xE),
                wiiMagicWord = reader.ReadUInt32(),
                gameCubeMagicWord = reader.ReadUInt32(),
                gameTitle = reader.ReadString(0x40),
                disableHashVerification = reader.ReadBoolean(),
                disableDecryption = reader.ReadBoolean()
            };
        }

        private WiiDiscPartitionInformation ReadPartitionInfo(BinaryReaderX reader)
        {
            return new WiiDiscPartitionInformation
            {
                partitionCount1 = reader.ReadInt32(),
                partitionOffset1 = reader.ReadInt32(),
                partitionCount2 = reader.ReadInt32(),
                partitionOffset2 = reader.ReadInt32(),
                partitionCount3 = reader.ReadInt32(),
                partitionOffset3 = reader.ReadInt32(),
                partitionCount4 = reader.ReadInt32(),
                partitionOffset4 = reader.ReadInt32()
            };
        }

        private WiiDiscPartitionEntry[] ReadPartitionEntries(BinaryReaderX reader, int count)
        {
            var result = new WiiDiscPartitionEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadPartitionEntry(reader);

            return result;
        }

        private WiiDiscPartitionEntry ReadPartitionEntry(BinaryReaderX reader)
        {
            return new WiiDiscPartitionEntry
            {
                offset = reader.ReadInt32(),
                type = reader.ReadInt32()
            };
        }

        private WiiDiscRegionSettings ReadSettings(BinaryReaderX reader)
        {
            return new WiiDiscRegionSettings
            {
                region = reader.ReadInt32(),
                zero0 = reader.ReadBytes(0xC),
                japanAgeRating = reader.ReadByte(),
                usaAgeRating = reader.ReadByte(),
                zero1 = reader.ReadByte(),
                germanAgeRating = reader.ReadByte(),
                pegiAgeRating = reader.ReadByte(),
                finlandAgeRating = reader.ReadByte(),
                portugalAgeRating = reader.ReadByte(),
                britainAgeRating = reader.ReadByte(),
                australiaAgeRating = reader.ReadByte(),
                koreaAgeRating = reader.ReadByte(),
                zero2 = reader.ReadBytes(6)
            };
        }

        private WiiDiscPartitionHeader ReadPartitionHeader(BinaryReaderX reader)
        {
            return new WiiDiscPartitionHeader
            {
                ticket = new WiiDiscPartitionTicket
                {
                    signatureType = reader.ReadInt32(),
                    signature = reader.ReadBytes(0x100),
                    padding = reader.ReadBytes(0x3C),
                    issuer = reader.ReadString(0x40),
                    ecdhData = reader.ReadBytes(0x3C),
                    zero0 = reader.ReadBytes(3),
                    encryptedTitleKey = reader.ReadBytes(0x10),
                    unk0 = reader.ReadByte(),
                    ticketId = reader.ReadBytes(8),
                    consoleId = reader.ReadInt32(),
                    titleId = reader.ReadBytes(8),
                    unk1 = reader.ReadInt16(),
                    ticketTitleVersion = reader.ReadInt16(),
                    permittedTitlesMask = reader.ReadUInt32(),
                    permitMask = reader.ReadUInt32(),
                    isTitleExportAllowed = reader.ReadBoolean(),
                    commonKeyIndex = reader.ReadByte(),
                    unk2 = reader.ReadBytes(0x30),
                    contentAccessPermissions = reader.ReadBytes(0x40),
                    zero1 = reader.ReadInt16(),
                    timeLimits =
                    [
                        ReadPartitionTimeLimit(reader),
                        ReadPartitionTimeLimit(reader),
                        ReadPartitionTimeLimit(reader),
                        ReadPartitionTimeLimit(reader),
                        ReadPartitionTimeLimit(reader),
                        ReadPartitionTimeLimit(reader),
                        ReadPartitionTimeLimit(reader),
                        ReadPartitionTimeLimit(reader)
                    ]
                },
                tmdOffset = reader.ReadInt32(),
                tmdSize = reader.ReadInt32(),
                certChainOffset = reader.ReadInt32(),
                certChainSize = reader.ReadInt32(),
                h3Offset = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                tmd = ReadPartitionTmd(reader)
            };
        }

        private WiiDiscPartitionTimeLimit ReadPartitionTimeLimit(BinaryReaderX reader)
        {
            return new WiiDiscPartitionTimeLimit
            {
                enableTimeLimit = reader.ReadInt32(),
                limitSeconds = reader.ReadInt32()
            };
        }

        private WiiDiscPartitionTmd ReadPartitionTmd(BinaryReaderX reader)
        {
            var tmd = new WiiDiscPartitionTmd
            {
                signatureType = reader.ReadInt32(),
                signature = reader.ReadBytes(0x100),
                padding = reader.ReadBytes(0x3C),
                issuer = reader.ReadString(0x40),
                version = reader.ReadByte(),
                caCrlVersion = reader.ReadByte(),
                signerCrlVersion = reader.ReadByte(),
                isVWii = reader.ReadBoolean(),
                iosVersion = reader.ReadInt64(),
                titleId = reader.ReadBytes(8),
                titleType = reader.ReadInt32(),
                groupId = reader.ReadInt16(),
                zero0 = reader.ReadInt16(),
                region = reader.ReadInt16(),
                ratings = reader.ReadBytes(0x10),
                zero1 = reader.ReadBytes(0xC),
                ipcMask = reader.ReadBytes(0xC),
                zero2 = reader.ReadBytes(0x12),
                accessRights = reader.ReadUInt32(),
                titleVersion = reader.ReadInt16(),
                contentCount = reader.ReadInt16(),
                bootIndex = reader.ReadInt16(),
                zero3 = reader.ReadInt16()
            };

            tmd.contents = new WiiDiscPartitionTmdContent[tmd.contentCount];

            for (var i = 0; i < tmd.contentCount; i++)
                tmd.contents[i] = ReadPartitionTmdContent(reader);

            return tmd;
        }

        private WiiDiscPartitionTmdContent ReadPartitionTmdContent(BinaryReaderX reader)
        {
            return new WiiDiscPartitionTmdContent
            {
                contentId = reader.ReadInt32(),
                index = reader.ReadInt16(),
                type = reader.ReadInt16(),
                size = reader.ReadInt64(),
                hash = reader.ReadBytes(0x14)
            };
        }
    }
}
