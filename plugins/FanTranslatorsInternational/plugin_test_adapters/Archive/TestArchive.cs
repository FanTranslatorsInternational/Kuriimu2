using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.FileSystem.Nodes.Physical;
using Kontract.Interfaces;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using plugin_test_adapters.Archive.Models;

namespace plugin_test_adapters.Archive
{
    [Export(typeof(IPlugin))]
    [PluginExtensionInfo("*.test")]
    public class TestArchive : IArchiveAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles
    {
        public bool Identify(StreamInfo file, BaseReadOnlyDirectoryNode fileSystem)
        {
            using (var br = new BinaryReaderX(file.FileData, LeaveOpen))
                return br.ReadString(8) == "ARC TEST";
        }

        public void Dispose()
        {
            foreach (var file in Files)
                file.FileData.Dispose();
            Files = null;
        }

        public bool LeaveOpen { get; set; }
        public void Load(StreamInfo input, BaseReadOnlyDirectoryNode fileSystem)
        {
            var dataStream = fileSystem.GetFileNode("archive.data").Open();

            using (var br = new BinaryReaderX(input.FileData, LeaveOpen))
            {
                var header = br.ReadType<Header>();
                var entries = br.ReadMultiple<FileEntry>(header.fileCount);

                Files = entries.Select(x => new ArchiveFileInfo
                {
                    FileName = x.name,
                    FileData = new SubStream(dataStream, x.offset, x.size),
                    PluginNames = new[] { "plugin_test_adapters.Archive.TestArchive" }
                }).ToList();
            }
        }

        public void Save(StreamInfo output, PhysicalDirectoryNode fileSystem, int versionIndex = 0)
        {
            fileSystem.AddFile("archive.data");
            var dataStream = fileSystem.GetFileNode("archive.data").Open();

            using (var bwData = new BinaryWriterX(dataStream, false))
            using (var bw = new BinaryWriterX(output.FileData, LeaveOpen))
            {
                var header = new Header { fileCount = Files.Count };
                var entries = new List<FileEntry>();

                var offset = 0;
                foreach (var file in Files)
                {
                    file.FileData.CopyTo(bwData.BaseStream);
                    entries.Add(new FileEntry
                    {
                        offset = offset,
                        size = (int)file.FileData.Length,
                        name = Path.GetFileName(file.FileName),
                        nameLength = Encoding.UTF8.GetByteCount(Path.GetFileName(file.FileName))
                    });

                    offset += (int)file.FileData.Length;
                }

                bw.WriteType(header);
                bw.WriteMultiple(entries);
            }
        }

        public List<ArchiveFileInfo> Files { get; private set; }
        public bool FileHasExtendedProperties => false;
    }
}
