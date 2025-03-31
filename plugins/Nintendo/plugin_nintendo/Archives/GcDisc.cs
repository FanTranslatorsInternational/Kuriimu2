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
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            var result = new List<IArchiveFile>();

            // Read header
            _header = typeReader.Read<GcDiscHeader>(br);

            // Special treatment for apploader size
            input.Position = 0x2440;
            var appLoader = typeReader.Read<GcAppLoader>(br);

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
            var typeWriter = new BinaryTypeWriter();
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
            typeWriter.WriteMany(entries.Select(x => x.Item1), bw);

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
            typeWriter.Write(_header, bw);
        }
    }
}
