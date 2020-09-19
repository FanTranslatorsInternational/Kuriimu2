using System;
using System.IO;
using Komponent.IO.Attributes;
using Komponent.IO.BinarySupport;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    class CiaHeader
    {
        public int headerSize;
        public short type;
        public short version;
        public int certChainSize;
        public int ticketSize;
        public int tmdSize;
        public int metaSize;
        public long contentSize;
        [FixedLength(0x2000)]
        public byte[] contentIndex;
    }

    class CiaCertificateChain
    {
        public CiaCertificate ca;
        public CiaCertificate tmdVerifier;
        public CiaCertificate ticketVerifier;
    }

    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    class CiaCertificate
    {
        public int sigType;

        [CalculateLength(typeof(CiaSupport), nameof(CiaSupport.GetSignatureLength))]
        public byte[] signature;
        [CalculateLength(typeof(CiaSupport), nameof(CiaSupport.GetSignaturePadding))]
        public byte[] signturePadding;

        [FixedLength(0x40)]
        public string issuer;

        public int keyType;

        [FixedLength(0x40)]
        public string name;

        [CalculateLength(typeof(CiaSupport), nameof(CiaSupport.GetPublicKeyLength))]
        public byte[] publicKey;
        public int unk1;
        [CalculateLength(typeof(CiaSupport), nameof(CiaSupport.GetPublicKeyPadding))]
        public byte[] publicKeyPadding;
    }

    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    class CiaTicket
    {
        public int sigType;

        [CalculateLength(typeof(CiaSupport), nameof(CiaSupport.GetSignatureLength))]
        public byte[] signature;
        [CalculateLength(typeof(CiaSupport), nameof(CiaSupport.GetSignaturePadding))]
        public byte[] signturePadding;

        public CiaTicketData CiaTicketData;
    }

    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    class CiaTicketData
    {
        [FixedLength(0x40)]
        public string issuer;
        [FixedLength(0x3C)]
        public byte[] eccPublicKey;
        public byte version;
        public byte caCrlVersion;
        public byte signerCrlVersion;
        [FixedLength(0x10)]
        public byte[] titleKey;
        public byte reserved1;
        public ulong ticketID;
        public uint consoleID;
        public ulong titleID;
        public short reserved2;
        public short ticketTitleVersion;
        public ulong reserved3;
        public byte licenseType;
        public byte keyYIndex;
        [FixedLength(0x2A)]
        public byte[] reserved4;
        public uint eshopAccID;
        public byte reserved5;
        public byte audit;
        [FixedLength(0x42)]
        public byte[] reserved6;
        [FixedLength(0x40)]
        public byte[] limits;
        [FixedLength(0xAC)]
        public byte[] contentIndex;
    }

    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    class CiaTmd
    {
        public int sigType;

        [CalculateLength(typeof(CiaSupport), nameof(CiaSupport.GetSignatureLength))]
        public byte[] signature;
        [CalculateLength(typeof(CiaSupport), nameof(CiaSupport.GetSignaturePadding))]
        public byte[] signturePadding;

        public CiaTmdHeader header;
        [FixedLength(0x40)]
        public CiaContentInfoRecord[] contentInfoRecords;
        [VariableLength("header.contentCount")]
        public CiaContentChunkRecord[] contentChunkRecords;
    }

    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    class CiaTmdHeader
    {
        [FixedLength(0x40)]
        public string issuer;
        public byte version;
        public byte caCrlVersion;
        public byte signerCrlVersion;
        public byte reserved1;
        public long systemVersion;
        public ulong titleID;
        public int titleType;
        public short groupID;
        public int saveDataSize;
        public int srlPrivateSaveDataSize;
        public int reserved2;
        public byte srlFlag;
        [FixedLength(0x31)]
        public byte[] reserved3;
        public int accessRights;
        public short titleVersion;
        public short contentCount;
        public short bootContent;
        public short padding;
        [FixedLength(0x20)]
        public byte[] sha256;
    }

    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    class CiaContentInfoRecord
    {
        public short contentChunkIndex;
        public short contentChunkCount;
        [FixedLength(0x20)]
        public byte[] sha256;
    }

    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    class CiaContentChunkRecord
    {
        public int contentID;
        public short contentIndex;
        public short contentType;
        public long contentSize;
        [FixedLength(0x20)]
        public byte[] sha256;
    }

    class CiaMeta
    {
        [FixedLength(0x180)]
        public byte[] titleIDDependency;
        [FixedLength(0x180)]
        public byte[] reserved1;
        public int coreVersion;
        [FixedLength(0xFC)]
        public byte[] reserved2;
        [FixedLength(0x36C0)]
        public byte[] iconData;
    }

    class CiaArchiveFileInfo : ArchiveFileInfo
    {
        public CiaContentChunkRecord ContentChunkRecord { get; }

        public CiaArchiveFileInfo(Stream fileData, string filePath, CiaContentChunkRecord contentChunkRecord) : 
            base(fileData, filePath)
        {
            ContentChunkRecord = contentChunkRecord;
        }

        public CiaArchiveFileInfo(Stream fileData, string filePath, 
            IKompressionConfiguration configuration, long decompressedSize, 
            CiaContentChunkRecord contentChunkRecord) : 
            base(fileData, filePath, configuration, decompressedSize)
        {
            ContentChunkRecord = contentChunkRecord;
        }
    }

    static class CiaSupport
    {
        public static int GetSignatureLength(ValueStorage storage)
        {
            var sigType = (int)storage.Get("sigType");
            switch (sigType)
            {
                case 0x010003:
                    return 0x200;

                case 0x010004:
                    return 0x100;

                case 0x010005:
                    return 0x3c;

                default:
                    throw new InvalidOperationException($"Unsupported signature type {sigType:X8}.");
            }
        }

        public static int GetSignaturePadding(ValueStorage storage)
        {
            var sigType = (int)storage.Get("sigType");
            switch (sigType)
            {
                case 0x010003:
                    return 0x3C;

                case 0x010004:
                    return 0x3C;

                case 0x010005:
                    return 0x40;

                default:
                    throw new InvalidOperationException($"Unsupported signature type {sigType:X8}.");
            }
        }

        public static int GetPublicKeyLength(ValueStorage storage)
        {
            var keyType = (int)storage.Get(nameof(CiaCertificate.keyType));
            switch (keyType)
            {
                case 0:
                    return 0x204;

                case 1:
                    return 0x104;

                case 2:
                    return 0x3C;

                default:
                    throw new InvalidOperationException($"Unsupported key type {keyType}.");
            }
        }

        public static int GetPublicKeyPadding(ValueStorage storage)
        {
            var keyType = (int)storage.Get(nameof(CiaCertificate.keyType));
            switch (keyType)
            {
                case 0:
                    return 0x34;

                case 1:
                    return 0x34;

                case 2:
                    return 0x3C;

                default:
                    throw new InvalidOperationException($"Unsupported key type {keyType}.");
            }
        }
    }
}
