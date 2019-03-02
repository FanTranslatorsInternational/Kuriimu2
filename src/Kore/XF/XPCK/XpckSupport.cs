using System.Runtime.InteropServices;
using System;
using System.IO;
using Komponent.IO;
using Kore.XFont.Interface;
using Komponent.IO.Attributes;

namespace Kore.XFont.Archive
{
    public class XPCKFileInfo : ArchiveFileInfo
    {
        public FileInfoEntry Entry;

        public int Write(Stream input, int absDataOffset, int baseDataOffset)
        {
            using (var bw = new BinaryWriterX(input, true))
            {
                bw.BaseStream.Position = absDataOffset;
                FileData.CopyTo(bw.BaseStream);
                if (bw.BaseStream.Position % 4 > 0) bw.WriteAlignment(4);
                else bw.WritePadding(4);

                var relOffset = absDataOffset - baseDataOffset;
                Entry.tmp = (ushort)((relOffset >> 2) & 0xffff);
                Entry.tmpZ = (byte)(((relOffset >> 2) & 0xff0000) >> 16);
                Entry.tmp2 = (ushort)(FileSize & 0xffff);
                Entry.tmp2Z = (byte)((FileSize & 0xff0000) >> 16);

                return ((absDataOffset + FileSize) % 4 > 0) ? (int)(absDataOffset + FileSize + 0x3) & ~0x3 : (int)(absDataOffset + FileSize + 4);
            }
        }
    }
    
    public struct XPCKHeader
    {
        [FixedLength(4)]
        public string magic;
        public byte fc1;
        public byte fc2;
        public ushort tmp1;
        public ushort tmp2;
        public ushort tmp3;
        public ushort tmp4;
        public ushort tmp5;
        public uint tmp6;

        public ushort fileCount => (ushort)((fc2 & 0xf) << 8 | fc1);
        public ushort fileInfoOffset => (ushort)(tmp1 << 2);
        public ushort filenameTableOffset => (ushort)(tmp2 << 2);
        public ushort dataOffset => (ushort)(tmp3 << 2);
        public ushort fileInfoSize => (ushort)(tmp4 << 2);
        public ushort filenameTableSize => (ushort)(tmp5 << 2);
        public uint dataSize => tmp6 << 2;
    }
    
    public struct FileInfoEntry
    {
        public uint crc32;
        public ushort nameOffset;
        public ushort tmp;
        public ushort tmp2;
        public byte tmpZ;
        public byte tmp2Z;

        public uint fileOffset => (((uint)tmpZ << 16) | tmp) << 2;
        public uint fileSize => ((uint)tmp2Z << 16) | tmp2;
    }
}