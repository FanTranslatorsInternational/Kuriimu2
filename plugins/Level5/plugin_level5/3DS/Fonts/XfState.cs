using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Font;
using Kontract.Models.Context;
using Kontract.Models.Font;
using Kontract.Models.IO;

namespace plugin_level5._3DS.Fonts
{
    public class XfState : IFontState, ILoadFiles, ISaveFiles, IAddCharacters, IRemoveCharacters
    {
        private readonly IPluginManager _pluginManager;
        private readonly Xf _xf;

        private bool _isChanged;
        private IStateInfo _archiveStateInfo;
        private IStateInfo _imageStateInfo;

        private List<CharacterInfo> _characters;

        public IReadOnlyList<CharacterInfo> Characters => _characters;
        public float Baseline { get => _xf.Header.largeCharHeight; set => _xf.Header.largeCharHeight = (short)value; }

        public float DescentLine { get => 0; set { } }

        public bool ContentChanged => IsChanged();

        public XfState(IPluginManager pluginManager)
        {
            _pluginManager = pluginManager;
            _xf = new Xf();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            // Load Xpck archive
            var loadResult = await _pluginManager.LoadFile(fileSystem, filePath, Guid.Parse("de276e88-fb2b-48a6-a55f-d6c14ec60d4f"));
            if (!loadResult.IsSuccessful)
                return;

            _archiveStateInfo = loadResult.LoadedState;
            var archiveState = _archiveStateInfo.PluginState as IArchiveState;

            // Load font image from archive
            var imageFile = archiveState.Files[0];
            loadResult = await _pluginManager.LoadFile(_archiveStateInfo, imageFile, Guid.Parse("898c9151-71bd-4638-8f90-6d34f0a8600c"));
            if (!loadResult.IsSuccessful)
                return;

            _imageStateInfo = loadResult.LoadedState;
            var imageState = _imageStateInfo.PluginState as IImageState;

            // Load KanvasImage
            var kanvasImage = new KanvasImage(imageState, imageState.Images[0]);
            var fntFile = await archiveState.Files[1].GetFileData(loadContext.TemporaryStreamManager, loadContext.ProgressContext);

            // Load characters
            _characters = await Task.Run(() => _xf.Load(fntFile, kanvasImage.GetImage(loadContext.ProgressContext)));
            _isChanged = false;
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            // Save font information
            var imageState = _imageStateInfo.PluginState as IImageState;
            var (fontStream, fontImage) = _xf.Save(_characters, imageState.Images[0].ImageSize);

            // Save image
            var kanvasImage = new KanvasImage(imageState, imageState.Images[0]);
            kanvasImage.SetImage((Bitmap)fontImage);

            var saveResult = await _pluginManager.SaveFile(_imageStateInfo);
            if (!saveResult.IsSuccessful)
                throw saveResult.Exception;

            // Set font file
            var archiveState = _archiveStateInfo.PluginState as IArchiveState;
            archiveState.Files[1].SetFileData(fontStream);

            // Save archive
            saveResult = await _pluginManager.SaveFile(_archiveStateInfo, fileSystem, savePath);
            if (!saveResult.IsSuccessful)
                throw saveResult.Exception;

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

        private bool IsChanged()
        {
            return _characters.Any(x => x.ContentChanged) || _isChanged;
        }
    }
}
