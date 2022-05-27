using Komponent.IO.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace plugin_capcom.Archives
{
    public class GtCPacHeader
    {
        public int tableOffset;
        public int tableSize;
    }
    public class GtCPacYEKBHeader
    {
        [FixedLength(14)]
        public string magic;
        public int dataOffset;
    }
    public class GtCPacYEKBFile
    {
        public int offset;
        public short size;
        public short unk2;
    }
    public class GtCRnan
    {
        [FixedLength(4)]
        public string magic;
        public short byteOrder;
        public short version;
        public int filesize;
        public short knbaChunkOffset;
        public short followingChunks;
    }
    public class GtcKnba
    {
        [FixedLength(4)]
        public string magic;
        public int chunkSize;
        public short animationBlockCount;
        public short frameBlockCount;
        public int offsetFromKnbaToAnimation;
        public int offsetFromKnbaToFrameBlock;
        public int offsetFromKnbaToFrameData;
        public long zero;
    }
    public class GtcKnbaAnimationBlocks
    {
        public int numberOfFrames;
        public short unk1;
        public short unk2;
        public int unk3;
        public int offsetFromFrameBlockToFirstFrame;
    }
    public class GtcKnbaFrameBlock
    {
        public int offsetFromFrameDataToUnk;
        public short frameWidth;
        public short unused;
    }
    public class GtcKnbaFrameData
    {
        public short unk1;
    }
    public class GtcLbal
    {
        [FixedLength(4)]
        public string magic;
        public int chunkSize;
        public int offsetsFromAnimationBlock;
    }
    public class GtcTxeu
    {
        [FixedLength(4)]
        public string magic;
        public int chunkSize;
        public int unk1;
    }
}
