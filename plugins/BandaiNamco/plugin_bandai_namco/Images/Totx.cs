using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models;

namespace plugin_bandai_namco.Images
{
    class Totx
    {
        private static readonly Guid CtpkId = Guid.Parse("5033920c-b6d9-4e44-8f3d-de8380cfce27");

        private IFileState _state;

        public IList<IKanvasImage> Load(Stream input, IBaseFileManager fileManager)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            var header = br.ReadType<TotxHeader>();

            // Read CTPK
            var ctpkState = fileManager.LoadFile(new StreamFile(new SubStream(input, 0x80, input.Length - 0x80), "file.ctpk"), CtpkId).Result;
            if (!ctpkState.IsSuccessful)
                throw new InvalidOperationException(ctpkState.Message);

            _state = ctpkState.LoadedFileState;
            var images = (_state.PluginState as IImageState).Images;

            // Edit image info
            images[0].ImageInfo.ImageSize = new Size(header.width, header.height);
            images[0].ImageInfo.PadSize.ToMultiple(8);

            return images;
        }

        public void Save(Stream output, IBaseFileManager fileManager)
        {
            var images = (_state.PluginState as IImageState).Images;

            using var bw = new BinaryWriterX(output);

            // Create header
            var header = new TotxHeader
            {
                width = (short)images[0].ImageSize.Width,
                height = (short)images[0].ImageSize.Height
            };

            // Prepare image info
            images[0].ImageInfo.ImageSize = images[0].ImageInfo.PadSize.Build(images[0].ImageSize);

            // Write CTPK
            var ctpkStream = _state.StateChanged ?
                fileManager.SaveStream(_state).Result.SavedStream[0].Stream :
                _state.FileSystem.OpenFile(_state.FilePath);

            ctpkStream.Position = 0;
            output.Position = 0x80;
            ctpkStream.CopyTo(output);

            // Write header
            output.Position = 0;
            bw.WriteType(header);

            // Finalize file manager
            fileManager.Close(_state);
            _state = null;
        }
    }
}
