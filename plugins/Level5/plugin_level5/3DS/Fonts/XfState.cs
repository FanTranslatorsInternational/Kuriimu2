using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Plugins.State.Features;
using Kontract.Interfaces.Plugins.State.Font;
using Kontract.Models.FileSystem;
using Kontract.Models.Managers.Files;
using Kontract.Models.Plugins.State;
using Kontract.Models.Plugins.State.Font;
using plugin_level5._3DS.Archives;

namespace plugin_level5._3DS.Fonts
{
    public class XfState : IFontState, ILoadFiles, ISaveFiles, IAddCharacters, IRemoveCharacters
    {
        private readonly IFileManager _pluginManager;
        private readonly Xf _xf;
        private readonly Xpck _xpck;

        private bool _isChanged;
        private List<IArchiveFileInfo> _xpckFiles;
        private IFileState _imageStateInfo;

        private List<CharacterInfo> _characters;

        public IReadOnlyList<CharacterInfo> Characters => _characters;
        public float Baseline { get => _xf.Header.largeCharHeight; set => _xf.Header.largeCharHeight = (short)value; }

        public float DescentLine { get => 0; set { } }

        public bool ContentChanged => _characters.Any(x => x.ContentChanged) || _isChanged;

        public XfState(IFileManager pluginManager)
        {
            _pluginManager = pluginManager;
            _xf = new Xf();
            _xpck = new Xpck();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            // Load archive
            _xpckFiles = _xpck.Load(await fileSystem.OpenFileAsync(filePath));

            // Load font image from archive
            var fileData = await _xpckFiles[0].GetFileData(loadContext.TemporaryStreamManager, loadContext.ProgressContext);
            var loadResult = await _pluginManager.LoadFile(new StreamFile(fileData, _xpckFiles[0].FilePath), Guid.Parse("898c9151-71bd-4638-8f90-6d34f0a8600c"));
            if (!loadResult.IsSuccessful)
                return;

            _imageStateInfo = loadResult.LoadedFileState;
            var imageState = _imageStateInfo.PluginState as IImageState;

            // Load KanvasImage
            var image = imageState.Images[0].GetImage(loadContext.ProgressContext);

            // Load characters
            var fntFile = await _xpckFiles[1].GetFileData(loadContext.TemporaryStreamManager, loadContext.ProgressContext);
            _characters = _xf.Load(fntFile, image);

            _isChanged = false;
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            // Save font information
            var imageState = _imageStateInfo.PluginState as IImageState;
            var (fontStream, fontImage) = _xf.Save(_characters, imageState.Images[0].ImageSize);

            // Save image
            imageState.Images[0].SetImage((Bitmap)fontImage);

            var saveResult = await _pluginManager.SaveStream(_imageStateInfo);
            if (!saveResult.IsSuccessful)
                throw saveResult.Exception;

            // Set file data
            _xpckFiles[0].SetFileData(saveResult.SavedStream[0].Stream);
            _xpckFiles[1].SetFileData(fontStream);

            // Save archive
            var output = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _xpck.Save(output, _xpckFiles);

            _isChanged = false;
        }

        public CharacterInfo CreateCharacterInfo(uint codePoint)
        {
            return new CharacterInfo(codePoint, Size.Empty, null);
        }

        public bool AddCharacter(CharacterInfo characterInfo)
        {
            if (characterInfo == null || _characters.Contains(characterInfo))
                return false;

            _characters.Add(characterInfo);
            _isChanged = true;

            return true;
        }

        public bool RemoveCharacter(CharacterInfo characterInfo)
        {
            if (!Characters.Contains(characterInfo))
                return false;

            _characters.Remove(characterInfo);
            _isChanged = true;

            return true;
        }

        public void RemoveAll()
        {
            _characters.Clear();
            _isChanged = true;
        }
    }
}
