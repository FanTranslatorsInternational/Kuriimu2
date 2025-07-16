using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    // Specification: https://www.gc-forever.com/yagcd/chap13.html#sec13
    // Files commonly denoted as 'boot.bin' and 'fst.bin' are internally managed by this plugin, and are therefore not exposed to the user
    class GcDisc
    {
        private GcDiscHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            var result = new List<IArchiveFile>();

            // Read header
            _header = ReadHeader(br);

            // Special treatment for apploader size
            input.Position = 0x2440;
            var appLoader = ReadAppLoader(br);

            // Collect system files
            result.Add(new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = "sys/bi2.bin",
                FileData = new SubStream(input, 0x440, 0x2000)
            }));
            result.Add(new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = "sys/appldr.bin",
                FileData = new SubStream(input, 0x2440, (appLoader.size + appLoader.trailerSize + 0x1F) & ~0x1F)
            }));
            result.Add(new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = "sys/main.dol",
                FileData = new SubStream(input, _header.execOffset, _header.fstOffset - _header.execOffset)
            }));

            // Collect file system files
            var u8 = new DefaultU8FileSystem(UPath.Root);
            result.AddRange(u8.Parse(input, _header.fstOffset, _header.fstSize, 0));

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            // Get system files
            var bi2File = files.First(x => x.FilePath.ToRelative() == "sys/bi2.bin");
            var appLoaderFile = files.First(x => x.FilePath.ToRelative() == "sys/appldr.bin");
            var execFile = files.First(x => x.FilePath.ToRelative() == "sys/main.dol");

            // Calculate offsets
            var bi2Offset = 0x440;
            var appLoaderOffset = bi2Offset + bi2File.FileSize;
            var execOffset = appLoaderOffset + ((appLoaderFile.FileSize + 0x1F) & ~0x1F) + 0x20;
            var fstOffset = (execOffset + execFile.FileSize + 0x1F) & ~0x1F;

            // Build U8 entry list
            var treeBuilder = new U8TreeBuilder(Encoding.ASCII);
            treeBuilder.Build(files.Where(x => !x.FilePath.IsInDirectory("/sys", false)).Select(x => (x.FilePath.FullName, x)).ToArray());

            var nameStream = treeBuilder.NameStream;
            var entries = treeBuilder.Entries;

            nameStream.Position = 0;

            // Write names
            var nameOffset = output.Position = fstOffset + treeBuilder.Entries.Count * 0xC;
            nameStream.CopyTo(output);
            bw.WriteAlignment(0x20);

            // Write files
            foreach (var (u8Entry, afi) in entries.Where(x => x.Item2 != null))
            {
                bw.WriteAlignment(0x20);
                var fileOffset = (int)bw.BaseStream.Position;

                var writtenSize = afi.WriteFileData(bw.BaseStream);

                u8Entry.offset = fileOffset;
                u8Entry.size = (int)writtenSize;
            }

            // Write FST
            output.Position = fstOffset;
            WriteEntries(entries.Select(x => x.Item1).ToArray(), bw);

            // Write system files
            output.Position = bi2Offset;
            bi2File.WriteFileData(output);

            output.Position = appLoaderOffset;
            appLoaderFile.WriteFileData(output);

            output.Position = execOffset;
            execFile.WriteFileData(output);

            // Write header
            _header.execOffset = (int)execOffset;
            _header.fstOffset = (int)fstOffset;
            _header.fstSize = (int)(nameStream.Length + (nameOffset - fstOffset));
            _header.fstMaxSize = _header.fstSize;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private GcDiscHeader ReadHeader(BinaryReaderX reader)
        {
            return new GcDiscHeader
            {
                gameCode = new GcDiscGameCode
                {
                    consoleId = reader.ReadByte(),
                    gameCode = reader.ReadInt16(),
                    countryCode = reader.ReadByte()
                },
                makerCode = reader.ReadInt16(),
                discId = reader.ReadByte(),
                version = reader.ReadByte(),
                audioStreamingEnabled = reader.ReadBoolean(),
                streamBufferSize = reader.ReadByte(),
                padding = reader.ReadBytes(0x12),
                magic = reader.ReadUInt32(),
                gameName = reader.ReadString(0x3e0),
                dhOffset = reader.ReadInt32(),
                dbgLoadAddress = reader.ReadInt32(),
                unused1 = reader.ReadBytes(0x18),
                execOffset = reader.ReadInt32(),
                fstOffset = reader.ReadInt32(),
                fstSize = reader.ReadInt32(),
                fstMaxSize = reader.ReadInt32(),
                userPosition = reader.ReadInt32(),
                userLength = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                unused2 = reader.ReadInt32()
            };
        }

        private GcAppLoader ReadAppLoader(BinaryReaderX reader)
        {
            return new GcAppLoader
            {
                date = reader.ReadString(0xA),
                padding = reader.ReadBytes(6),
                entryPoint = reader.ReadInt32(),
                size = reader.ReadInt32(),
                trailerSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(GcDiscHeader header, BinaryWriterX writer)
        {
            writer.Write(header.gameCode.consoleId);
            writer.Write(header.gameCode.gameCode);
            writer.Write(header.gameCode.countryCode);

            writer.Write(header.makerCode);
            writer.Write(header.discId);
            writer.Write(header.version);
            writer.Write(header.audioStreamingEnabled);
            writer.Write(header.streamBufferSize);
            writer.Write(header.padding);
            writer.Write(header.magic);
            writer.WriteString(header.gameName, writeNullTerminator: false);
            writer.Write(header.dhOffset);
            writer.Write(header.dbgLoadAddress);
            writer.Write(header.unused1);
            writer.Write(header.execOffset);
            writer.Write(header.fstOffset);
            writer.Write(header.fstSize);
            writer.Write(header.fstMaxSize);
            writer.Write(header.userPosition);
            writer.Write(header.userLength);
            writer.Write(header.unk1);
            writer.Write(header.unused2);
        }

        private void WriteEntries(U8Entry[] entries, BinaryWriterX writer)
        {
            foreach (U8Entry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(U8Entry entry, BinaryWriterX writer)
        {
            writer.Write(entry.tmp1);
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
