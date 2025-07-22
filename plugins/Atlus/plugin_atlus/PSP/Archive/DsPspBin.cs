using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_atlus.PSP.Archive

{
    class DsPspBin
    {
        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            var sizeList = new List<int>();

            // Read file count
            int fileCount = br.ReadInt32();

            // Read sizes
            for (var i = 0; i < fileCount; i++)
                sizeList.Add(br.ReadInt32());

            // Add files
            int dataPosition = (fileCount * 4 + 4 + 0xF) & ~0xF;

            var result = new List<IArchiveFile>();
            for (var i = 0; i < fileCount; i++)
            {
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = $"{i:X8}.bin",
                    FileData = new SubStream(input, dataPosition, sizeList[i])
                }));

                dataPosition = (dataPosition + sizeList[i] + 0xF) & ~0xF;
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Write data
            output.Position = (files.Count * 4 + 4 + 0xF) & ~0xF;

            var sizeList = new List<int>();
            foreach (IArchiveFile file in files)
            {
                var writtenSize = (int)file.WriteFileData(bw.BaseStream, false);
                sizeList.Add(writtenSize);

                bw.WriteAlignment(0x10);
            }

            // Write fileCount
            output.Position = 0;
            bw.Write(files.Count);

            // Write sizes
            foreach (int size in sizeList)
                bw.Write(size);
        }
    }
}
