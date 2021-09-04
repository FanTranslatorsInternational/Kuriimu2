using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_mercury_steam.Images
{
    class MtxtState : IImageState, ILoadFiles, ISaveFiles
    {
        private IBaseFileManager _fileManager;
        private IFileState _ctpkState;

        private MtxtHeader _header;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public MtxtState(IBaseFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream, true);

            // Read header
            _header = br.ReadType<MtxtHeader>();

            // Read name
            fileStream.Position = _header.offset + _header.imgSize;
            var name = br.ReadCStringASCII();

            // Load CTPK
            var ctpkStream = new SubStream(fileStream, _header.offset, _header.imgSize);
            var loadResult = await _fileManager.LoadFile(new StreamFile(ctpkStream, name + ".ctpk"));
            if (!loadResult.IsSuccessful || !(loadResult.LoadedFileState.PluginState is IImageState))
                throw new InvalidOperationException(loadResult.Message);

            // Read CTPK
            _ctpkState = loadResult.LoadedFileState;
            Images = ((IImageState)_ctpkState.PluginState).Images;
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriterX(fileStream);

            // Save CTPK
            var saveResult = await _fileManager.SaveStream(_ctpkState);
            if (!saveResult.IsSuccessful)
                throw new InvalidOperationException(saveResult.Message);

            // Update header
            _header.width = Images[0].ImageSize.Width;
            _header.height = Images[0].ImageSize.Height;

            _header.imgSize = (int)saveResult.SavedStream[0].Stream.Length;
            _header.nameOffset = _header.offset + _header.imgSize;

            // Write header
            bw.WriteType(_header);

            // Write CTPK
            fileStream.Position = _header.offset;
            saveResult.SavedStream[0].Stream.CopyTo(fileStream);

            // Write name
            bw.WriteString(saveResult.SavedStream[0].Path.GetNameWithoutExtension(), Encoding.ASCII, false);
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ContentChanged);
        }
    }
}
