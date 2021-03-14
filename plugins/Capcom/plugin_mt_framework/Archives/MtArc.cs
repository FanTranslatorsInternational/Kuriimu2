using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_mt_framework.Archives
{
    class MtArc
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(MtHeader));

        private MtHeader _header;
        private ByteOrder _byteOrder;
        private Platform _platform;

        private bool _isEncrypted;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            _isEncrypted = IsEncrypted(input);
            var key = GetCipherKey("imaguy_uyrag_igurustim_", "enokok_ikorodo_odohuran");

            using var br = new BinaryReaderX(input, true);

            // Determine byte order
            var magic = br.ReadString(4);
            br.ByteOrder = _byteOrder = magic == "\0CRA" ? ByteOrder.BigEndian : ByteOrder.LittleEndian;
            input.Position -= 4;

            // Header
            _header = br.ReadType<MtHeader>();

            // Determine possible platform the arc was found on
            _platform = MtArcSupport.DeterminePlatform(_byteOrder, _header);

            // Skip additional int under certain conditions
            if (_byteOrder == ByteOrder.LittleEndian && _header.version != 7 && _header.version != 8 && _header.version != 9)
                br.ReadInt32();

            // Read entries
            Stream entryStream = new SubStream(input, br.BaseStream.Position, input.Length - br.BaseStream.Position);
            if (_isEncrypted)
                entryStream = new MtBlowfishStream(entryStream, key);

            using var entryBr = new BinaryReaderX(entryStream, _byteOrder);
            var entries = entryBr.ReadMultiple(_header.entryCount, _header.version == 9 ? typeof(MtEntrySwitch) : typeof(MtEntry)).Cast<IMtEntry>();

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                Stream subStream = new SubStream(input, entry.Offset, entry.CompSize);
                var fileName = entry.FileName.TrimEnd('\0') + MtArcSupport.DetermineExtension(entry.ExtensionHash);

                if (_isEncrypted)
                    subStream = new MtBlowfishStream(subStream, key);

                result.Add(CreateAfi(subStream, fileName, entry, _platform));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var key = GetCipherKey("imaguy_uyrag_igurustim_", "enokok_ikorodo_odohuran");

            using var bw = new BinaryWriterX(output, _byteOrder);

            var isExtendedHeader = _byteOrder == ByteOrder.LittleEndian && _header.version != 7 && _header.version != 8 && _header.version != 9;

            // Calculate offsets
            var entryOffset = HeaderSize + (isExtendedHeader ? 4 : 0);
            var fileOffset = MtArcSupport.DetermineFileOffset(_byteOrder, _header.version, files.Count, entryOffset);

            // Write files
            var entries = new List<IMtEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<MtArchiveFileInfo>())
            {
                output.Position = filePosition;

                long writtenSize;
                if (!_isEncrypted)
                    writtenSize = file.SaveFileData(output);
                else
                {
                    var fileStream = file.GetFinalStream();

                    var ms = new MemoryStream();
                    var encryptedStream = new MtBlowfishStream(ms, key);

                    fileStream.CopyTo(encryptedStream);

                    ms.Position = 0;
                    ms.CopyTo(output);

                    writtenSize = fileStream.Length;
                }

                file.Entry.Offset = filePosition;
                file.Entry.SetDecompressedSize((int)file.FileSize, _platform);
                file.Entry.CompSize = (int)writtenSize;
                entries.Add(file.Entry);

                filePosition += (int)writtenSize;
            }

            // Write entries
            Stream entryStream = new SubStream(output, entryOffset, output.Length - entryOffset);
            if (_isEncrypted)
                entryStream = new MtBlowfishStream(entryStream, key);

            using var entryBw = new BinaryWriterX(entryStream, _byteOrder);
            entryBw.WriteMultiple(entries);

            // Write header
            _header.entryCount = (short)files.Count;

            output.Position = 0;
            bw.WriteType(_header);
        }

        private bool IsEncrypted(Stream input)
        {
            input.Position = 8;

            var buffer = new byte[0x40];
            input.Read(buffer, 0, 0x40);

            // Check if first entry name looks encrypted
            var isEncrypted = buffer.Any(x => x > 0x7F || x > 0 && x < 0x20);

            input.Position = 0;
            return isEncrypted;
        }

        private byte[] GetCipherKey(string key1, string key2) => key1.Reverse().Select((c, i) => (byte)(c ^ key2[i] | i << 6)).ToArray();

        private IArchiveFileInfo CreateAfi(Stream file, string fileName, IMtEntry entry, Platform platform)
        {
            // It seems every file is compressed with ZLib on Switch
            // Reasoning: Example file game.arc contains of at least one file "om120a" where compressed and uncompressed size are equal but the file is still compressed
            //            the decompressed file is really the same size; comparing with other entries no clear differences were found, that would indicate a
            //            compression flag
            if (platform == Platform.SWITCH)
                return new MtArchiveFileInfo(file, fileName, entry, Kompression.Implementations.Compressions.ZLib, entry.GetDecompressedSize(platform));

            if (entry.CompSize != entry.GetDecompressedSize(platform))
            {
                var compMagic = file.ReadByte();
                if ((compMagic & 0xF) != 8 || (compMagic & 0xF0) > 0x70)
                    throw new InvalidOperationException("File is marked as compressed but doesn't use ZLib.");

                return new MtArchiveFileInfo(file, fileName, entry, Kompression.Implementations.Compressions.ZLib, entry.GetDecompressedSize(platform));
            }

            return new MtArchiveFileInfo(file, fileName, entry);
        }
    }
}
