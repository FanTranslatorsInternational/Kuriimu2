using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models;
using Kontract.Models.Image;
using Kontract.Models.IO;
using plugin_headstrong_games.Archives;

namespace plugin_headstrong_games.Images
{
    class Fabtex
    {
        private static Guid CtpkId => Guid.Parse("5033920c-b6d9-4e44-8f3d-de8380cfce27");

        private FabNode _root;
        private IFileState _ctpkState;

        public IList<IKanvasImage> Load(Stream input, IFileManager fileManager)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read FBRC tree
            _root = FabNode.Read(br);
            var dataNode = _root.Nodes.FirstOrDefault(x => x.Type == "PDAT");

            // Read CTPK
            var result = fileManager.LoadFile(new StreamFile(dataNode.Data, "file.ctpk"), CtpkId).Result;
            if (!result.IsSuccessful)
                throw new InvalidOperationException(result.Message);

            _ctpkState = result.LoadedFileState;
            return (_ctpkState.PluginState as IImageState).Images;
        }

        public void Save(Stream output, IFileManager fileManager)
        {
            var imageState = _ctpkState.PluginState as IImageState;
            var buffer = new byte[4];

            // Save CTPK
            var ctpkStream = _ctpkState.StateChanged
                ? fileManager.SaveStream(_ctpkState).Result.SavedStream[0].Stream
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
