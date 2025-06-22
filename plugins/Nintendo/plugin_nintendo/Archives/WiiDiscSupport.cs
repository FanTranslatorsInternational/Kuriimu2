using Komponent.Contract.Aspects;
using Konnect.Contract.DataClasses.FileSystem;

namespace plugin_nintendo.Archives
{
    struct WiiDiscHeader
    {
        public byte wiiDiscId;
        public short gameCode;
        public byte regionCode;
        public short makerCode;
        public byte discNumber;
        public byte discVersion;
        public bool enableAudioStreaming;
        public byte streamBufferSize;

        public byte[] zero0;

        public uint wiiMagicWord;       // 0x5D1C9EA3
        public uint gameCubeMagicWord;  // 0xC2339F3D

        public string gameTitle;

        public bool disableHashVerification;
        public bool disableDecryption;
    }

    struct WiiDiscPartitionInformation
    {
        public int partitionCount1;
        public int partitionOffset1;
        public int partitionCount2;
        public int partitionOffset2;
        public int partitionCount3;
        public int partitionOffset3;
        public int partitionCount4;
        public int partitionOffset4;
    }

    struct WiiDiscPartitionEntry
    {
        public int offset;
        public int type;
    }

    struct WiiDiscRegionSettings
    {
        public int region;

        public byte[] zero0;

        public byte japanAgeRating;
        public byte usaAgeRating;

        public byte zero1;

        public byte germanAgeRating;
        public byte pegiAgeRating;
        public byte finlandAgeRating;
        public byte portugalAgeRating;
        public byte britainAgeRating;
        public byte australiaAgeRating;
        public byte koreaAgeRating;

        public byte[] zero2;
    }

    struct WiiDiscPartitionHeader
    {
        public WiiDiscPartitionTicket ticket;
        public int tmdOffset;
        public int tmdSize;
        public int certChainOffset;
        public int certChainSize;
        public int h3Offset;
        public int dataOffset;
        public int dataSize;
        public WiiDiscPartitionTmd tmd;
    }

    struct WiiDiscPartitionTicket
    {
        public int signatureType;
        public byte[] signature;
        public byte[] padding;
        public string issuer;
        public byte[] ecdhData;
        public byte[] zero0;
        public byte[] encryptedTitleKey;
        public byte unk0;
        public byte[] ticketId;
        public int consoleId;
        public byte[] titleId;
        public short unk1;
        public short ticketTitleVersion;
        public uint permittedTitlesMask;
        public uint permitMask;
        public bool isTitleExportAllowed;
        public byte commonKeyIndex;
        public byte[] unk2;
        public byte[] contentAccessPermissions;
        public short zero1;
        public WiiDiscPartitionTimeLimit[] timeLimits;
    }

    struct WiiDiscPartitionTimeLimit
    {
        public int enableTimeLimit;
        public int limitSeconds;
    }

    struct WiiDiscPartitionTmd
    {
        public int signatureType;
        public byte[] signature;
        public byte[] padding;
        public string issuer;
        public byte version;
        public byte caCrlVersion;
        public byte signerCrlVersion;
        public bool isVWii;
        public long iosVersion;
        public byte[] titleId;
        public int titleType;
        public short groupId;
        public short zero0;
        public short region;
        public byte[] ratings;
        public byte[] zero1;
        public byte[] ipcMask;
        public byte[] zero2;
        public uint accessRights;
        public short titleVersion;
        public short contentCount;
        public short bootIndex;
        public short zero3;
        public WiiDiscPartitionTmdContent[] contents;
    }

    struct WiiDiscPartitionTmdContent
    {
        public int contentId;
        public short index;
        public short type;
        public long size;
        [FixedLength(0x14)]
        public byte[] hash;
    }

    class WiiDiscU8FileSystem : BaseU8FileSystem
    {
        public WiiDiscU8FileSystem(UPath root) : base(root)
        {
        }

        protected override long GetFileOffset(int offset)
        {
            return (long)offset << 2;
        }
    }

    class WiiDiscPartitionDataStream : Stream
    {
        private const int BlockSize_ = 0x8000;
        private const int HashBlockSize_ = 0x400;
        private const int DataBlockSize_ = 0x7C00;

        private readonly Stream _baseStream;

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _baseStream.Length / BlockSize_ * DataBlockSize_;
        public override long Position { get; set; }

        public WiiDiscPartitionDataStream(Stream baseStream)
        {
            if (baseStream.Length % BlockSize_ != 0)
                throw new InvalidOperationException($"The given stream needs to be aligned to 0x{BlockSize_:X4}");

            _baseStream = baseStream;
        }

        public override void Flush()
        {
            // TODO: Flush after write
            _baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return Position = offset;

                case SeekOrigin.Current:
                    return Position += offset;

                case SeekOrigin.End:
                    return Position = Length + offset;
            }

            throw new ArgumentException("Origin is invalid.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bkPos = _baseStream.Position;

            var readBytes = 0;
            while (count > 0 && Position < Length)
                readBytes += ReadNextDataBlock(buffer, ref offset, ref count);

            _baseStream.Position = bkPos;
            return readBytes;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        private int ReadNextDataBlock(byte[] buffer, ref int offset, ref int count)
        {
            var blockPosition = Position % DataBlockSize_ + HashBlockSize_;
            var blockStart = Position / DataBlockSize_ * BlockSize_;

            var length = (int)Math.Min(BlockSize_ - blockPosition, count);
            _baseStream.Position = blockStart + blockPosition;
            var readBytes = _baseStream.Read(buffer, offset, length);

            Position += length;
            offset += length;
            count -= length;

            return readBytes;
        }
    }
}
