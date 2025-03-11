using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Reflection.PortableExecutable;
using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_atlus.PS2.Archive
{
    public class DdtImg
    {
        private const int Alignment_ = 0x800;

        private static readonly Encoding EucJpEncoding = Encoding.GetEncoding("EUC-JP");
        private static readonly int EntrySize = 12;

        public List<ArchiveFileInfo> Load(Stream ddtStream, Stream imgStream)
        {
            using var br = new BinaryReaderX(ddtStream);
            var files = EnumerateFiles(br, imgStream, UPath.Root).OfType<ArchiveFileInfo>().ToList();
            return files;
        }

        public void Save(Stream ddtStream, Stream imgStream, List<ArchiveFile> files)
        {
        }

        private IEnumerable<IArchiveFile> EnumerateFiles(BinaryReaderX br, Stream imgStream, UPath currentPath)
        {
            var typeReader = new BinaryTypeReader();

            // Read current entry
            var entry = typeReader.Read<DdtEntry>(br);

            // Read name
            br.BaseStream.Position = entry.nameOffset;
            var name = br.ReadNullTerminatedString();

            // If entry is a file
            if (entry.entrySize >= 0)
            {
                var subStream = new SubStream(imgStream, entry.entryOffset * Alignment_, entry.entrySize);

                yield return new ArchiveFileInfo
                {
                    FilePath = (currentPath / name).FullName,
                    FileData = subStream
                } as IArchiveFile;
                yield break;
            }

            // If entry is a directory
            for (var i = 0; i < -entry.entrySize; i++)
            {
                br.BaseStream.Position = entry.entryOffset + i * EntrySize;
                foreach (var file in EnumerateFiles(br, imgStream, currentPath / name))
                    yield return file;
            }
        }
    }
}
