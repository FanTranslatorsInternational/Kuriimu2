using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;

namespace Level5.Fonts
{
    [Export(typeof(IFontAdapter2))]
    [PluginInfo("33B42E7E-FFA6-4F8D-B30A-D0334910BC45", "Level 5 Font", "XF", "onepiecefreak", "", "This is the XF font adapter for Kuriimu.")]
    [PluginExtensionInfo("*.xf")]
    public sealed class XfAdapter : IFontAdapter2, IIdentifyFiles, ILoadFiles, IAddCharacters, IDeleteCharacters
    {
        private XF _xf;

        #region Properties

        public List<FontCharacter2> Characters
        {
            get => _xf.Characters;
            set => _xf.Characters = value.Select(fc => fc).ToList();
        }

        public float Baseline
        {
            get => _xf.Header.baseLine;
            set => _xf.Header.baseLine = (short)value;
        }

        public float DescentLine
        {
            get => _xf.Header.descentLine;
            set => _xf.Header.descentLine = (short)value;
        }

        #endregion

        //public void Save(string filename, int versionIndex = 0)
        //{
        //    _xf.Save(File.OpenWrite(filename));
        //}

        public bool AddCharacter(FontCharacter2 character)
        {
            return false;
        }

        public bool DeleteCharacter(FontCharacter2 character)
        {
            if (!_xf.Characters.Contains(character))
                return false;

            _xf.Characters.Remove(character);
            return true;
        }

        public void Dispose()
        {
            _xf = null;
        }

        public bool Identify(StreamInfo file, BaseReadOnlyDirectoryNode fileSystem)
        {
            using (var br = new BinaryReaderX(file.FileData, LeaveOpen))
            {
                var magic = br.ReadString(4, Encoding.ASCII);
                return magic == "XPCK";
            }
        }

        public void Load(StreamInfo input, BaseReadOnlyDirectoryNode fileSystem)
        {
            _xf = new XF(input.FileData);
        }

        public bool LeaveOpen { get; set; }
    }
}
