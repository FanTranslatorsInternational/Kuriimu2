
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using Komponent.IO;
using Kontract.Attribute;
using Kontract.Interface;

namespace Kore.SamplePlugins
{
    [Export(typeof(GfdAdapter))]
    [Export(typeof(IFontAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("3C8827B8-D124-45D7-BD4C-2A98E049A20A", "MT Framework Font", "GFD", "IcySon55", "This is the GFD text adapter for Kuriimu.")]
    [PluginExtensionInfo("*.gfd")]
    public sealed class GfdAdapter : IFontAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles, IAddCharacters, IDeleteCharacters
    {
        private GFD _gfd;

        #region Properties

        public IEnumerable<FontCharacter> Characters => _gfd?.Characters;

        public List<Bitmap> Textures { get; private set; }

        public float BaseLine
        {
            get => _gfd.Header.BaseLine;
            set => _gfd.Header.BaseLine = value;
        }

        public float DescentLine
        {
            get => _gfd.Header.DescentLine;
            set => _gfd.Header.DescentLine = value;
        }

        #endregion

        public bool Identify(string filename)
        {
            var result = true;

            try
            {
                using (var br = new BinaryReaderX(File.OpenRead(filename)))
                {
                    var magic = br.ReadString(4);
                    if (!magic.StartsWith("GFD") && !magic.StartsWith("\0DFG"))
                        result = false;
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        public void Load(string filename)
        {
            if (File.Exists(filename))
            {
                _gfd = new GFD(File.OpenRead(filename));

                var textureFiles = Directory.GetFiles(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "*.png");

                Textures = new List<Bitmap>();
                foreach (var file in textureFiles)
                    Textures.Add((Bitmap)Image.FromFile(file));
            }
            else
                throw new FileNotFoundException();
        }

        public void Save(string filename)
        {
            _gfd.Save(File.Create(filename));
        }

        public FontCharacter NewCharacter()
        {
            throw new NotImplementedException();
        }

        public bool AddCharacter(FontCharacter character)
        {
            throw new NotImplementedException();
        }

        public bool DeleteCharacter(FontCharacter character)
        {
            throw new NotImplementedException();
        }
    }
}
