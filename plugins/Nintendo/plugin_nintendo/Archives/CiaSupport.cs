using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

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
        public byte[] contentIndex;
    }

    class CiaCertificateChain
    {
        public CiaCertificate ca;
        public CiaCertificate tmdVerifier;
        public CiaCertificate ticketVerifier;
    }

    class CiaCertificate
    {
        public int sigType;
        public byte[] signature;
        public byte[] signaturePadding;
        public string issuer;
        public int keyType;
        public string name;
        public byte[] publicKey;
        public int unk1;
        public byte[] publicKeyPadding;
    }

    class CiaTicket
    {
        public int sigType;
        public byte[] signature;
        public byte[] signaturePadding;
        public CiaTicketData ticketData;
    }

    class CiaTicketData
    {
        public string issuer;
        public byte[] eccPublicKey;
        public byte version;
        public byte caCrlVersion;
        public byte signerCrlVersion;
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
        public byte[] reserved4;
        public uint eshopAccID;
        public byte reserved5;
        public byte audit;
        public byte[] reserved6;
        public byte[] limits;
        public byte[] contentIndex;
    }

    class CiaTmd
    {
        public int sigType;
        public byte[] signature;
        public byte[] signaturePadding;
        public CiaTmdHeader header;
        public CiaContentInfoRecord[] contentInfoRecords;
        public CiaContentChunkRecord[] contentChunkRecords;
    }

    class CiaTmdHeader
    {
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
        public byte[] reserved3;
        public int accessRights;
        public short titleVersion;
        public short contentCount;
        public short bootContent;
        public short padding;
        public byte[] sha256;
    }

    class CiaContentInfoRecord
    {
        public short contentChunkIndex;
        public short contentChunkCount;
        public byte[] sha256;
    }

    class CiaContentChunkRecord
    {
        public int contentID;
        public short contentIndex;
        public short contentType;
        public long contentSize;
        public byte[] sha256;
    }

    class CiaMeta
    {
        public byte[] titleIDDependency;
        public byte[] reserved1;
        public int coreVersion;
        public byte[] reserved2;
        public byte[] iconData;
    }

    class CiaArchiveFile : ArchiveFile
    {
        public CiaContentChunkRecord ContentChunkRecord { get; }

        public CiaArchiveFile(ArchiveFileInfo fileInfo, CiaContentChunkRecord contentChunkRecord) : base(fileInfo)
        {
            ContentChunkRecord = contentChunkRecord;
        }
    }

    static class CiaSupport
    {
        public static int GetSignatureLength(int sigType)
        {
            switch (sigType)
            {
                case 0x010003:
                    return 0x200;

                case 0x010004:
                    return 0x100;

                case 0x010005:
                    return 0x3c;

                default:
                    throw new InvalidOperationException($"Unsupported signature type {sigType:X8} for length.");
            }
        }

        public static int GetSignaturePadding(int sigType)
        {
            switch (sigType)
            {
                case 0x010003:
                    return 0x3C;

                case 0x010004:
                    return 0x3C;

                case 0x010005:
                    return 0x40;

                default:
                    throw new InvalidOperationException($"Unsupported signature type {sigType:X8} for padding.");
            }
        }

        public static int GetPublicKeyLength(int keyType)
        {
            switch (keyType)
            {
                case 0:
                    return 0x204;

                case 1:
                    return 0x104;

                case 2:
                    return 0x3C;

                default:
                    throw new InvalidOperationException($"Unsupported key type {keyType} for length.");
            }
        }

        public static int GetPublicKeyPadding(int keyType)
        {
            switch (keyType)
            {
                case 0:
                    return 0x34;

                case 1:
                    return 0x34;

                case 2:
                    return 0x3C;

                default:
                    throw new InvalidOperationException($"Unsupported key type {keyType} for padding.");
            }
        }
    }
}
