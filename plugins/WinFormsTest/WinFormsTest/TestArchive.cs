using Kontract.Attributes;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.VirtualFS;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsTest
{
    [Export(typeof(IArchiveAdapter))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(IMultipleFiles))]
    [PluginExtensionInfo("*.archive")]
    [PluginInfo("Test-Archive-Id")]
    public class TestArchive : IArchiveAdapter, ILoadFiles, IIdentifyFiles, IMultipleFiles
    {
        public List<ArchiveFileInfo> Files { get; private set; }

        public IVirtualFSRoot FileSystem { get; set; }

        public bool CanRenameFiles => false;

        public bool CanReplaceFiles => false;

        public bool FileHasExtendedProperties => false;

        public bool LeaveOpen { get; set; }

        public void Dispose()
        {
            ;
        }

        public bool Identify(StreamInfo file)
        {
            using (var br = new BinaryReader(file.FileData, Encoding.ASCII, LeaveOpen))
                return br.ReadUInt32() == 0x16161617;
        }

        public void Load(StreamInfo file)
        {
            using (var br = new BinaryReader(file.FileData, Encoding.ASCII, true))
            {
                br.BaseStream.Position = 4;

                var fileCount = br.ReadInt16();
                Files = new List<ArchiveFileInfo>();
                for (int i = 0; i < fileCount; i++)
                {
                    var length = br.ReadInt32();
                    Files.Add(new ArchiveFileInfo
                    {
                        State = ArchiveFileState.Archived,
                        FileName = Encoding.ASCII.GetString(br.ReadBytes(0x20)).TrimEnd('\0'),
                        FileData = new MemoryStream(br.ReadBytes(length))
                    });
                }

                if (fileCount == 3)
                {
                    var otherFile = FileSystem.GetDirectory("thesecondfolder").OpenFile("other.bin", FileMode.Open);
                    using (var br1 = new BinaryReader(otherFile))
                        if (br1.ReadByte() != 0x22)
                            throw new InvalidOperationException("other.bin failed check");
                }
            }
        }
    }
}
