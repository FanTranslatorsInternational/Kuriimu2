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
        private MtArcPlatform _platform;

        public IList<IArchiveFileInfo> Load(Stream input, MtArcPlatform platform)
        {
            _platform = platform;

            switch (platform)
            {
                case MtArcPlatform.LittleEndian:
                    return LoadLittleEndian(input);

                case MtArcPlatform.BigEndian:
                    return LoadBigEndian(input);

                case MtArcPlatform.Switch:
                    return LoadSwitch(input);

                default:
                    throw new InvalidOperationException();
            }

            //_isEncrypted = IsEncrypted(input);
            //var key = GetCipherKey("imaguy_uyrag_igurustim_", "enokok_ikorodo_odohuran");

            //using var br = new BinaryReaderX(input, true);

            //// Determine byte order
            //var magic = br.ReadString(4);
            //br.ByteOrder = _byteOrder = magic == "\0CRA" ? ByteOrder.BigEndian : ByteOrder.LittleEndian;
            //input.Position -= 4;

            //// Header
            //_header = br.ReadType<MtHeader>();

            //// Determine possible mtArcPlatform the arc was found on
            //_mtArcPlatform = MtArcSupport.DeterminePlatform(_byteOrder, _header);

            //// Skip additional int under certain conditions
            //if (_byteOrder == ByteOrder.LittleEndian && _header.version != 7 && _header.version != 8 && _header.version != 9)
            //    br.ReadInt32();

            //// Read entries
            //Stream entryStream = new SubStream(input, br.BaseStream.Position, input.Length - br.BaseStream.Position);
            //if (_isEncrypted)
            //    entryStream = new MtBlowfishStream(entryStream, key);

            //using var entryBr = new BinaryReaderX(entryStream, _byteOrder);
            //var entries = entryBr.ReadMultiple(_header.entryCount, _header.version == 9 ? typeof(MtEntrySwitch) : typeof(MtEntry)).Cast<IMtEntry>();

            //// Add files
            //var result = new List<IArchiveFileInfo>();
            //foreach (var entry in entries)
            //{
            //    Stream subStream = new SubStream(input, entry.Offset, entry.CompSize);
            //    var fileName = entry.FileName.TrimEnd('\0') + MtArcSupport.DetermineExtension(entry.ExtensionHash);

            //    if (_isEncrypted)
            //        subStream = new MtBlowfishStream(subStream, key);

            //    result.Add(CreateAfi(subStream, fileName, entry, _mtArcPlatform));
            //}

            //return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            switch (_platform)
            {
                case MtArcPlatform.LittleEndian:
                    SaveLittleEndian(output, files);
                    break;

                case MtArcPlatform.BigEndian:
                    SaveBigEndian(output, files);
                    break;

                case MtArcPlatform.Switch:
                    SaveSwitch(output, files);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            //var key = GetCipherKey("imaguy_uyrag_igurustim_", "enokok_ikorodo_odohuran");

            //using var bw = new BinaryWriterX(output, _byteOrder);

            //var isExtendedHeader = _byteOrder == ByteOrder.LittleEndian && _header.version != 7 && _header.version != 8 && _header.version != 9;

            //// Calculate offsets
            //var entryOffset = HeaderSize + (isExtendedHeader ? 4 : 0);
            //var fileOffset = MtArcSupport.DetermineFileOffset(_byteOrder, _header.version, files.Count, entryOffset);

            //// Write files
            //var entries = new List<IMtEntry>();

            //var filePosition = fileOffset;
            //foreach (var file in files.Cast<MtArchiveFileInfo>())
            //{
            //    output.Position = filePosition;

            //    long writtenSize;
            //    if (!_isEncrypted)
            //        writtenSize = file.SaveFileData(output);
            //    else
            //    {
            //        var fileStream = file.GetFinalStream();

            //        var ms = new MemoryStream();
            //        var encryptedStream = new MtBlowfishStream(ms, key);

            //        fileStream.CopyTo(encryptedStream);

            //        ms.Position = 0;
            //        ms.CopyTo(output);

            //        writtenSize = fileStream.Length;
            //    }

            //    file.Entry.Offset = filePosition;
            //    file.Entry.SetDecompressedSize((int)file.FileSize, _mtArcPlatform);
            //    file.Entry.CompSize = (int)writtenSize;
            //    entries.Add(file.Entry);

            //    filePosition += (int)writtenSize;
            //}

            //// Write entries
            //Stream entryStream = new SubStream(output, entryOffset, output.Length - entryOffset);
            //if (_isEncrypted)
            //    entryStream = new MtBlowfishStream(entryStream, key);

            //using var entryBw = new BinaryWriterX(entryStream, _byteOrder);
            //entryBw.WriteMultiple(entries);

            //// Write header
            //_header.entryCount = (short)files.Count;

            //output.Position = 0;
            //bw.WriteType(_header);
        }

        #region Load

        private IList<IArchiveFileInfo> LoadLittleEndian(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<MtHeader>();

            // Skip additional int under certain conditions
            if (_header.version != 7 && _header.version != 8)
                br.ReadInt32();

            // Read entries
            var entries = br.ReadMultiple<MtEntry>(_header.entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.Offset, entry.CompSize);
                var fileName = entry.FileName.TrimEnd('\0') + MtArcSupport.DetermineExtension(entry.ExtensionHash);

                result.Add(CreateAfi(fileStream, fileName, entry, _platform));
            }

            return result;
        }

        private IList<IArchiveFileInfo> LoadBigEndian(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            _header = br.ReadType<MtHeader>();

            // Read entries
            var entries = br.ReadMultiple<MtEntry>(_header.entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.Offset, entry.CompSize);
                var fileName = entry.FileName.TrimEnd('\0') + MtArcSupport.DetermineExtension(entry.ExtensionHash);

                result.Add(CreateAfi(fileStream, fileName, entry, _platform));
            }

            return result;
        }

        private IList<IArchiveFileInfo> LoadSwitch(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<MtHeader>();

            // Read entries
            var entries = br.ReadMultiple<MtEntrySwitch>(_header.entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.Offset, entry.CompSize);
                var fileName = entry.FileName.TrimEnd('\0') + MtArcSupport.DetermineExtension(entry.ExtensionHash);

                // It seems every file is compressed with ZLib on Switch
                // Reasoning: Example file game.arc contains of at least one file "om120a" where compressed and uncompressed size are equal but the file is still compressed
                //            the decompressed file is really the same size; comparing with other entries no clear differences were found, that would indicate a
                //            compression flag
                result.Add(new MtArchiveFileInfo(fileStream, fileName, entry, Kompression.Implementations.Compressions.ZLib, entry.GetDecompressedSize(_platform)));
            }

            return result;
        }

        #endregion

        #region Save

        private void SaveLittleEndian(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            var isExtendedHeader = _header.version != 7 && _header.version != 8;

            // Calculate offsets
            var entryOffset = HeaderSize + (isExtendedHeader ? 4 : 0);
            var fileOffset = MtArcSupport.DetermineFileOffset(ByteOrder.LittleEndian, _header.version, files.Count, entryOffset);

            // Write files
            var entries = new List<IMtEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<MtArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                file.Entry.Offset = filePosition;
                file.Entry.SetDecompressedSize((int)file.FileSize, _platform);
                file.Entry.CompSize = (int)writtenSize;
                entries.Add(file.Entry);

                filePosition += (int)writtenSize;
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            _header.entryCount = (short)files.Count;

            output.Position = 0;
            bw.WriteType(_header);
        }

        private void SaveBigEndian(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = MtArcSupport.DetermineFileOffset(ByteOrder.BigEndian, _header.version, files.Count, entryOffset);

            // Write files
            var entries = new List<IMtEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<MtArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                file.Entry.Offset = filePosition;
                file.Entry.SetDecompressedSize((int)file.FileSize, _platform);
                file.Entry.CompSize = (int)writtenSize;
                entries.Add(file.Entry);

                filePosition += (int)writtenSize;
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            _header.entryCount = (short)files.Count;

            output.Position = 0;
            bw.WriteType(_header);
        }

        private void SaveSwitch(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = MtArcSupport.DetermineFileOffset(ByteOrder.LittleEndian, _header.version, files.Count, entryOffset);

            // Write files
            var entries = new List<IMtEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<MtArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                file.Entry.Offset = filePosition;
                file.Entry.SetDecompressedSize((int)file.FileSize, _platform);
                file.Entry.CompSize = (int)writtenSize;
                entries.Add(file.Entry);

                filePosition += (int)writtenSize;
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            _header.entryCount = (short)files.Count;

            output.Position = 0;
            bw.WriteType(_header);
        }

        #endregion

        public int GetArchiveSize(IList<IArchiveFileInfo> files, ByteOrder byteOrder)
        {
            // Get header size
            var isExtendedHeader = _header.version != 7 && _header.version != 8;
            var headerSize = HeaderSize + (isExtendedHeader ? 4 : 0);

            // Get file offset
            var fileOffset = MtArcSupport.DetermineFileOffset(byteOrder, _header.version, files.Count, headerSize);

            // Add file sizes
            var fileRegionSize = (int)files.Cast<MtArchiveFileInfo>().Sum(x => x.GetFinalStream().Length);

            return fileOffset + fileRegionSize;
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

        private IArchiveFileInfo CreateAfi(Stream file, string fileName, IMtEntry entry, MtArcPlatform mtArcPlatform)
        {
            if (entry.CompSize == entry.GetDecompressedSize(mtArcPlatform))
                return new MtArchiveFileInfo(file, fileName, entry);

            var compMagic = file.ReadByte();
            if ((compMagic & 0xF) != 8 || (compMagic & 0xF0) > 0x70)
                throw new InvalidOperationException("File is marked as compressed but doesn't use ZLib.");

            return new MtArchiveFileInfo(file, fileName, entry, Kompression.Implementations.Compressions.ZLib, entry.GetDecompressedSize(mtArcPlatform));

        }
    }
}
