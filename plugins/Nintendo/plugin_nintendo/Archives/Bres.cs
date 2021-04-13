//using System.Collections.Generic;
//using System.IO;
//using Komponent.IO;
//using Komponent.IO.Streams;
//using Kontract.Models.Archive;
//using Kontract.Models.IO;

//namespace plugin_nintendo.Archives
//{
//    class Bres
//    {
//        private BresFile _bresFile;

//        public IList<IArchiveFileInfo> Load(Stream input)
//        {
//            _bresFile = new BresFile(input);

//            // Add files
//            var result = new List<IArchiveFileInfo>();
//            foreach (var file in _tree.CollectFiles())
//            {
//                var fileStream = new SubStream(input, file.DataOffset, file.DataSize);
//                var name = file.GetFullPath().FullName;

//                result.Add(new ArchiveFileInfo(fileStream, name));
//            }

//            return result;
//        }

//        public void Save(Stream output, IList<IArchiveFileInfo> files)
//        {
//            using var bw = new BinaryWriterX(output, _byteOrder);
//        }

//        private IEnumerable<IArchiveFileInfo> CollectAfi(Stream input, BresGroupNode groupNode, UPath path)
//        {
//            if (groupNode.Name != null)
//                path /= groupNode.Name;

//            foreach (var parsedNode in groupNode.ParsedNodes)
//            {
//                if (parsedNode is BresGroupNode localGroup)
//                    foreach (var afi in CollectAfi(input, localGroup, path))
//                        yield return afi;
//                else
//                    yield return new ArchiveFileInfo(new SubStream(input, (parsedNode as BresFile).DataOffset, (parsedNode as BresFile).DataSize), (path / (parsedNode as BresFile).Name).FullName);
//            }
//        }
//    }
//}
