using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Image;
using plugin_headstrong_games.Archives;

namespace plugin_headstrong_games.Images
{
    class Fabtex
    {
        private static Guid CtpkId => Guid.Parse("5033920c-b6d9-4e44-8f3d-de8380cfce27");

        private FabNode _root;
        private IFileState _ctpkState;

        public IReadOnlyList<IImageFile> Load(Stream input, IPluginFileManager fileManager)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read FBRC tree
            _root = FabNode.Read(br);
            var dataNode = _root.Nodes.FirstOrDefault(x => x.Type == "PDAT");

            // Read CTPK
            var result = fileManager.LoadFile(new StreamFile
            {
                Path = "file.ctpk",
                Stream = dataNode.Data
            }, CtpkId).Result;
            if (result.Status is not LoadStatus.Successful)
                throw new InvalidOperationException(result.Reason.ToString());

            _ctpkState = result.LoadedFileState;
            return (_ctpkState.PluginState as IImageFilePluginState).Images;
        }

        public void Save(Stream output, IPluginFileManager fileManager)
        {
            // Save CTPK
            var ctpkStream = _ctpkState.StateChanged
                ? fileManager.SaveStream(_ctpkState).Result.SavedStreams[0].Stream
                : _ctpkState.FileSystem.OpenFile(_ctpkState.FilePath);

            // Set saved CTPK
            var dataNode = _root.Nodes.FirstOrDefault(x => x.Type == "PDAT");
            dataNode.Data = ctpkStream;

            // Save node tree
            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);
            _root.Write(bw);

            // Clean CTPK state
            fileManager.Close(_ctpkState);
            _ctpkState = null;
        }
    }
}
