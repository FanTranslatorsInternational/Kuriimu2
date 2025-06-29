using System.Buffers.Binary;
using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_headstrong_games.Archives
{
    class Fab
    {
        private FabNode _root;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read node tree
            _root = FabNode.Read(br);

            // Read files
            var result = new List<IArchiveFile>();
            foreach (var fileBranch in _root.Nodes.Where(x => x.SubType == "FILE"))
            {
                var fileName = fileBranch.Nodes.FirstOrDefault(x => x.Type == "NAME")?.AsString();
                if (result.Any(x => x.FilePath.ToRelative().FullName == fileName))
                    continue;

                var fileDataNode = fileBranch.Nodes.FirstOrDefault(x => x.SubType == "DATA");
                var userNode = fileDataNode?.Nodes.FirstOrDefault(x => x.Type == "USER");

                var relevantNode = userNode ?? fileDataNode;
                var fileStream = relevantNode?.Data;

                if (userNode?.SubType == "LZ4C")
                    result.Add(new FabArchiveFile(new CompressedArchiveFileInfo
                    {
                        FilePath = fileName,
                        FileData = fileStream,
                        Compression = Compressions.Lz4Headerless.Build(),
                        DecompressedSize = PeekDecompressedLength(fileStream)
                    }, relevantNode));
                else
                    result.Add(new FabArchiveFile(new ArchiveFileInfo
                    {
                        FilePath = fileName,
                        FileData = fileStream
                    }, relevantNode));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            // Replace changed file data in nodes
            foreach (var file in files.Where(x => x.ContentChanged).Cast<FabArchiveFile>())
            {
                // This also re-compresses the changed file, if a compression is attached
                var ms = new MemoryStream();
                file.WriteFileData(ms, true);

                ms.Position = 0;
                file.DataNode.Data = ms;
            }

            // Write node tree
            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);
            _root.Write(bw);
        }

        private int PeekDecompressedLength(Stream input)
        {
            var result = 0;
            var startPosition = input.Position;

            var buffer = new byte[4];
            while (input.Position < input.Length)
            {
                input.Read(buffer);
                var decompSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);
                input.Read(buffer);
                var compSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);

                input.Position += compSize;

                if (decompSize < 0)
                    decompSize = ~decompSize + 1;
                result += decompSize;
            }

            input.Position = startPosition;
            return result;
        }
    }
}
