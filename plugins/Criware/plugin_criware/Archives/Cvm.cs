using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.Extensions;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_criware.Archives
{
    class Cvm
    {
        private const int EntrySize_ = 0x21;

        private static readonly IList<string> Passwords = new List<string>
        {
            "zxcv",
            "cc2fuku",
            "shinobutan",
            "MELTYBLOOD_AA",
            "PJ234110",
            "4147a5c2b5fe0357",
            "tinaandluckandru",
            "SAGUCHIFUNAYOI",
            "itinenmotanai",
            "qi2o@9a!"
        };

        private CvmHeader _header;
        private CvmZoneInfo _zoneInfo;
        private byte[] _unkDataLoc;
        private IsoPrimaryDescriptor _primeDesc;
        private string _detectedPassword;

        #region Load

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            // Prepare binary readers
            using var br = new BinaryReaderX(input, true);

            var isoStream = new SubStream(input, 0x1800, input.Length - 0x1800);

            // Read header
            _header = br.ReadType<CvmHeader>();
            _zoneInfo = br.ReadType<CvmZoneInfo>();
            _unkDataLoc = br.ReadBytes(0x800);

            // Prepare decryption stream
            Stream decStream = isoStream;
            if (_header.IsEncrypted)
            {
                _detectedPassword = DetectPassword(isoStream);
                decStream = new RofsCryptoStream(isoStream, _detectedPassword, 0, 0x800);
            }

            using var decBr = new BinaryReaderX(decStream);

            // Read ISO primary descriptor
            decStream.Position = 0x8000;
            _primeDesc = decBr.ReadType<IsoPrimaryDescriptor>();

            // Read record tree
            return ParseDirTree(decBr, isoStream, _primeDesc.rootDirRecord.extentLe, _primeDesc.rootDirRecord.sizeLe, UPath.Root).ToArray();
        }

        private IEnumerable<IArchiveFileInfo> ParseDirTree(BinaryReaderX br, Stream isoStream, uint dirExtent, uint dirSize, UPath path)
        {
            long currentPosition = dirExtent * 0x800;
            while (dirSize > 0)
            {
                var dirChunk = Math.Min(0x800, dirSize);
                dirSize -= dirChunk;

                while (dirChunk > 0)
                {
                    br.BaseStream.Position = currentPosition;

                    // Check if alignment to next sector is needed
                    var length = br.ReadByte();
                    if (length == 0)
                    {
                        br.SeekAlignment(0x800);
                        currentPosition = br.BaseStream.Position;
                        break;
                    }

                    // Read dir entry
                    br.BaseStream.Position--;
                    var dirRecord = br.ReadType<IsoDirectoryRecord>();
                    currentPosition = br.BaseStream.Position;

                    if ((dirRecord.flags & 2) > 0)
                    {
                        if (dirRecord.nameLength != 1 || dirRecord.name[0] != '\0' && dirRecord.name[0] != 1)
                            foreach (var file in ParseDirTree(br, isoStream, dirRecord.extentLe, dirRecord.sizeLe, path / dirRecord.name))
                                yield return file;
                    }
                    else
                    {
                        yield return new ArchiveFileInfo(new SubStream(isoStream, dirRecord.extentLe * 0x800, dirRecord.sizeLe), (path / dirRecord.name.Split(';')[0]).FullName);
                    }

                    dirChunk -= dirRecord.length;
                }
            }
        }

        #endregion

        #region Save

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var fileTree = files.ToTree();

            // Pre-calculate size of the TOC
            var dirTotalSize = CalculateDirTreeSize(fileTree);
            var dirOnlySize = CalculateDirTreeSize(fileTree, false);

            // Prepare streams
            output.SetLength(0xB800 + dirTotalSize);
            Stream tocStream = new SubStream(output, 0x1800, 0xA000 + dirTotalSize);
            if (_header.IsEncrypted)
                tocStream = new RofsCryptoStream(tocStream, _detectedPassword, 0, 0x800);

            // Write files and TOC
            using var tocBw = new BinaryWriterX(tocStream);
            var fileOffset = (long)(0xB800 + dirTotalSize);
            WriteDirTree(fileTree, output, tocBw, 0xA000, ref fileOffset);

            // Write pre TOC information
            _primeDesc.volSizeBe = (int)((output.Length - 0x1800 + 0x7FF) & ~0x7FF);
            _primeDesc.volSizeLe = (int)((output.Length - 0x1800 + 0x7FF) & ~0x7FF);
            _primeDesc.logicalBlockSizeLe = 0x800;
            _primeDesc.logicalBlockSizeBe = 0x800;
            _primeDesc.rootDirRecord.sizeLe = (uint)dirOnlySize;
            _primeDesc.rootDirRecord.sizeBe = (uint)dirOnlySize;

            tocStream.Position = 0x8000;
            tocBw.WriteType(_primeDesc);
            tocBw.WriteAlignment(0x800);

            // Write end ISO sector
            tocBw.Write((byte)0xFF);
            tocBw.WriteString("CD001", Encoding.ASCII, false, false);
            tocBw.Write((short)0x1);
            tocBw.WriteAlignment(0x800);

            // Write root dir information in little endian
            tocBw.Write((short)0x1);
            tocBw.Write(0x14);
            tocBw.Write((short)1);
            tocBw.WriteAlignment(0x800);

            // Write root dir information in big endian
            tocBw.Write((short)0x1);
            tocBw.ByteOrder = ByteOrder.BigEndian;
            tocBw.Write(0x14);
            tocBw.Write((short)1);

            // Write CVM header information
            _header.fileSize = output.Length;
            _zoneInfo.isoDataLoc.length = output.Length - 0x1800;

            using var bw = new BinaryWriterX(output);
            output.Position = 0;
            bw.WriteType(_header);
            bw.WriteType(_zoneInfo);
            bw.Write(_unkDataLoc);
        }

        private int CalculateDirTreeSize(DirectoryEntry dirEntry, bool calculateSubDirs = true, bool first = true)
        {
            var totalSize = 0;
            var sectorFilled = first ? (EntrySize_ + dirEntry.Name.Length + 1) & ~1 : 0;

            // First calculate all file entries of the directory
            foreach (var file in dirEntry.Files)
            {
                var entrySize = (EntrySize_ + file.FilePath.ToRelative().FullName.Length + 2 + 1) & ~1;
                if (sectorFilled + entrySize >= 0x800)
                {
                    totalSize += 0x800;
                    sectorFilled = 0;
                }

                sectorFilled += entrySize;
            }

            // Then calculate all directory entries
            foreach (var dir in dirEntry.Directories)
            {
                var entrySize = (EntrySize_ + dir.Name.Length + 1) & ~1;
                if (sectorFilled + entrySize >= 0x800)
                {
                    totalSize += 0x800;
                    sectorFilled = 0;
                }

                sectorFilled += entrySize;

                if (calculateSubDirs)
                    totalSize += CalculateDirTreeSize(dir, true, false);
            }

            if (sectorFilled != 0)
                totalSize += 0x800;

            return totalSize;
        }

        private void WriteDirTree(DirectoryEntry dirEntry, Stream input, BinaryWriterX tocBw, long entryOffset, ref long fileOffset, bool first = true)
        {
            var totalSize = 0;
            var sectorFilled = first ? (EntrySize_ + dirEntry.Name.Length + 1) & ~1 : 0;

            // Write file entries
            tocBw.BaseStream.Position = entryOffset + totalSize + sectorFilled;
            foreach (var file in dirEntry.Files.Cast<ArchiveFileInfo>())
            {
                var entrySize = (EntrySize_ + file.FilePath.GetName().Length + 3) & ~1;

                // Write file
                input.Position = fileOffset;
                file.SaveFileData(input);
                while (input.Position % 0x800 != 0)
                    input.WriteByte(0);

                // Advance positioning
                if (sectorFilled + entrySize >= 0x800)
                {
                    totalSize += 0x800;
                    sectorFilled = 0;
                    tocBw.WriteAlignment(0x800);
                }

                // Write file entry
                tocBw.BaseStream.Position = entryOffset + totalSize + sectorFilled;
                tocBw.WriteType(new IsoDirectoryRecord
                {
                    length = (byte)entrySize,
                    extentBe = (uint)((fileOffset - 0x1800) / 0x800),
                    extentLe = (uint)((fileOffset - 0x1800) / 0x800),
                    sizeBe = (uint)file.FileSize,
                    sizeLe = (uint)file.FileSize,
                    date = new byte[7],
                    flags = 0,
                    volumeSequenceNumber = 0x10000001,
                    nameLength = (byte)(file.FilePath.GetName().Length + 2),
                    name = file.FilePath.GetName() + ";1"
                });

                sectorFilled += entrySize;
                fileOffset += (file.FileSize + 0x7FF) & ~0x7FF;
            }

            // Calculate first sub dir offset
            var subDirOffset = totalSize;
            var sectorFilled2 = sectorFilled;
            foreach (var dir in dirEntry.Directories)
            {
                var entrySize = (EntrySize_ + dir.Name.Length + 1) & ~1;
                if (sectorFilled2 + entrySize >= 0x800)
                {
                    subDirOffset += 0x800;
                    sectorFilled2 = 0;
                }

                sectorFilled2 += entrySize;
            }

            if (sectorFilled2 != 0)
                subDirOffset += 0x800;

            // Write directory entries
            foreach (var dir in dirEntry.Directories)
            {
                var dirSize = CalculateDirTreeSize(dir, false);
                var entrySize = (EntrySize_ + dir.Name.Length + 1) & ~1;

                // Write sub directory
                WriteDirTree(dir, input, tocBw, entryOffset + subDirOffset, ref fileOffset, false);

                // Advance positioning
                if (sectorFilled + entrySize >= 0x800)
                {
                    totalSize += 0x800;
                    sectorFilled = 0;
                    tocBw.WriteAlignment(0x800);
                }

                // Write sub directory entry
                tocBw.BaseStream.Position = entryOffset + totalSize + sectorFilled;
                tocBw.WriteType(new IsoDirectoryRecord
                {
                    length = (byte)entrySize,
                    extentBe = (uint)((entryOffset + subDirOffset) / 0x800),
                    extentLe = (uint)((entryOffset + subDirOffset) / 0x800),
                    sizeBe = (uint)dirSize,
                    sizeLe = (uint)dirSize,
                    date = new byte[7],
                    flags = 2,
                    volumeSequenceNumber = 0x10000001,
                    nameLength = (byte)dir.Name.Length,
                    name = dir.Name
                });

                sectorFilled += entrySize;
                subDirOffset += dirSize;
            }

            if (sectorFilled != 0)
            {
                totalSize += 0x800;
                tocBw.WriteAlignment(0x800);
            }

            // Write current directory entry
            if (first)
            {
                var bkPos = tocBw.BaseStream.Position;

                tocBw.BaseStream.Position = entryOffset;
                tocBw.WriteType(new IsoDirectoryRecord
                {
                    length = (byte)((EntrySize_ + dirEntry.Name.Length + 1) & ~1),
                    extentBe = (uint)(entryOffset / 0x800),
                    extentLe = (uint)(entryOffset / 0x800),
                    sizeBe = (uint)totalSize,
                    sizeLe = (uint)totalSize,
                    date = new byte[7],
                    flags = 2,
                    volumeSequenceNumber = 0x10000001,
                    nameLength = (byte)(dirEntry.Name.Length == 0 ? 1 : dirEntry.Name.Length),
                    name = string.IsNullOrEmpty(dirEntry.Name) ? "\0" : dirEntry.Name
                });

                tocBw.BaseStream.Position = bkPos;
            }
        }

        #endregion

        private string DetectPassword(Stream input)
        {
            input = new SubStream(input, 0x8000, 0x800);

            foreach (var pw in Passwords)
            {
                using var cipher = new RofsCryptoStream(input, pw, 0x10, 0x800);
                using var cipherBr = new BinaryReaderX(cipher);

                cipher.Position = 1;
                if (cipherBr.ReadString(5) == "CD001")
                    return pw;
            }

            throw new InvalidOperationException("Password could not be detected. Please report this on the github of the developers of Kuriimu2.");
        }
    }
}
