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
    [PluginExtensionInfo("*.archiveinfo")]
    [PluginInfo("Test-Archive-Id")]
    public class TestArchive : IArchiveAdapter, ILoadFiles, IIdentifyFiles, IMultipleFiles, ISaveFiles
    {
        public List<ArchiveFileInfo> Files { get; private set; }

        public IVirtualFSRoot FileSystem { get; set; }

        public bool CanRenameFiles => false;

        public bool CanReplaceFiles => true;

        public bool FileHasExtendedProperties => false;

        public bool LeaveOpen { get; set; }

        public void Dispose()
        {
            ;
        }

        public bool Identify(StreamInfo file)
        {
            return true;
        }

        public void Load(StreamInfo file)
        {
            using (var br = new BinaryReader(file.FileData, Encoding.ASCII, LeaveOpen))
            {
                var fileCount = br.ReadInt32();
                br.BaseStream.Position = 0x10;

                // Get archive data file
                var archiveData = FileSystem.OpenFile(Path.GetFileNameWithoutExtension(file.FileName) + ".archive");

                using (var archiveBr = new BinaryReader(archiveData, Encoding.ASCII, LeaveOpen))
                {
                    Files = new List<ArchiveFileInfo>();
                    for (int i = 0; i < fileCount; i++)
                    {
                        var offset = br.ReadInt32();
                        var length = br.ReadInt32();
                        var name = Encoding.ASCII.GetString(br.ReadBytes(0x18)).TrimEnd('\0');

                        archiveBr.BaseStream.Position = offset;
                        Files.Add(new ArchiveFileInfo
                        {
                            FileData = new MemoryStream(archiveBr.ReadBytes(length)),
                            State = ArchiveFileState.Archived,
                            FileName = name
                        });
                    }
                }

                //if (fileCount == 3)
                //{
                //    var otherFile = FileSystem.GetDirectory("thesecondfolder2").OpenFile("other.bin");
                //    using (var br1 = new BinaryReader(otherFile))
                //        if (br1.ReadByte() != 0x22)
                //            throw new InvalidOperationException("other.bin failed check");
                //}
            }
        }

        public void Save(StreamInfo initialFile, int versionIndex = 0)
        {
            using (var bw = new BinaryWriter(initialFile.FileData, Encoding.ASCII, LeaveOpen))
            using (var bwData = new BinaryWriter(FileSystem.CreateFile(Path.GetFileNameWithoutExtension(initialFile.FileName) + ".archive"), Encoding.ASCII, LeaveOpen))
            {
                foreach (var file in Files)
                {
                    bw.Write((int)bwData.BaseStream.Position);
                    bw.Write((int)file.FileSize);
                    bw.Write(Encoding.ASCII.GetBytes(file.FileName));
                    while (bw.BaseStream.Position % 0x20 != 0)
                        bw.Write((byte)0);

                    file.FileData.Position = 0;
                    file.FileData.CopyTo(bwData.BaseStream);
                }
            }
        }
    }
}
