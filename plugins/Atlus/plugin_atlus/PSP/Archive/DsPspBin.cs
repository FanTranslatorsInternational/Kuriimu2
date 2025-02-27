using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_atlus.PSP.Archive

{
    class DsPspBin
    {
        public List<ArchiveFileInfo> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, leaveOpen: true);

            var sizeList = new List<int>();

            // Read fileCount
            int fileCount = br.ReadInt32();

            // Read pointers
            int entryPosition = fileCount * sizeof(int);

            // Read sizes
            for (int i = 0; i < fileCount; i++)
            {
                sizeList.Add(br.ReadInt32());
            }

            // Add files
            var result = new List<ArchiveFileInfo>();
            for (int i = 0; i < fileCount; i++)
            {
                var fileStream = new SubStream(input, entryPosition, sizeList[i]);
                string name = $"{i:X8}.bin";
                entryPosition += sizeList[i];
                
                result.Add(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = fileStream
                });
            }

            return result;
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculations
            int entryPosition = files.Count * sizeof(int);

            // Write fileCount
            bw.Write(files.Count);

            // Write data
            output.Position = entryPosition;
            var sizeList = new List<int>();
            foreach (ArchiveFileInfo file in files)
            {
                ArchiveFile archive = new ArchiveFile(file);
                var writtenSize = archive.WriteFileData(bw.BaseStream, false);
                sizeList.Add((int)writtenSize);
            }

            // Write sizes
            output.Position = 0x4;
            foreach (int size in sizeList)
            {
                bw.Write(size);
            }
        }
    }
}
