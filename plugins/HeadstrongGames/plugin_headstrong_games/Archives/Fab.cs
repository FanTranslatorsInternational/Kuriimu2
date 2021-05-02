using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using K4os.Compression.LZ4.Encoders;
using Komponent.IO;
using Kompression.Implementations;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_headstrong_games.Archives
{
    class Fab
    {
        private FabNode _root;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read node tree
            _root = FabNode.Read(br);

            // Read files
            var result = new List<IArchiveFileInfo>();
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
                    result.Add(new FabArchiveFileInfo(fileStream, fileName, Compressions.Lz4Headerless, PeekDecompressedLength(fileStream)) { DataNode = relevantNode });
                else
                    result.Add(new FabArchiveFileInfo(fileStream, fileName) { DataNode = relevantNode });
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            // Replace changed file data in nodes
            foreach (var file in files.Where(x => x.ContentChanged).Cast<FabArchiveFileInfo>())
            {
                // This also re-compresses the changed file, if a compression is attached
                var ms = new MemoryStream();
                file.SaveFileData(ms);

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
