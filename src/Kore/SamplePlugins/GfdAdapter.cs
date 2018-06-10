using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;

namespace Kore.SamplePlugins
{
    [Export(typeof(GfdAdapter))]
    [Export(typeof(IFontAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("3C8827B8-D124-45D7-BD4C-2A98E049A20A", "MT Framework Font", "GFD", "IcySon55", "", "This is the GFD font adapter for Kuriimu.")]
    [PluginExtensionInfo("*.gfd")]
    public sealed class GfdAdapter : IFontAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles, IAddCharacters, IDeleteCharacters
    {
        private GFD _gfd;

        #region Properties

        public IEnumerable<FontCharacter> Characters
        {
            get => _gfd?.Characters;
            set => _gfd.Characters = value.Select(fc => (GfdCharacter)fc).ToList();
        }

        public List<Bitmap> Textures { get; set; }

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
            //for (var i = 0; i < Textures.Count; i++)
            //{
            //    var tex = Textures[i];
            //    tex.Save(Path.Combine(Path.GetDirectoryName(filename), $"{Path.GetFileNameWithoutExtension(filename)}_{i:00}.png"), ImageFormat.Png);
            //}
        }

        public FontCharacter NewCharacter(FontCharacter selectedCharacter = null)
        {
            var newChar = new GfdCharacter();

            if (selectedCharacter is GfdCharacter gfd)
            {
                newChar.Block2Trailer = gfd.Block2Trailer;
                newChar.CharacterKerning = gfd.CharacterKerning;
                newChar.CharacterUnknown = gfd.CharacterUnknown;
                newChar.Block3Trailer = gfd.Block3Trailer;
            }

            return newChar;
        }

        public bool AddCharacter(FontCharacter character)
        {
            if (!(character is GfdCharacter gfdCharacter)) return false;
            _gfd.Characters.Add(gfdCharacter);
            _gfd.Characters.Sort((l, r) => l.Character.CompareTo(r.Character));
            return true;
        }

        public bool DeleteCharacter(FontCharacter character)
        {
            if (!(character is GfdCharacter gfdCharacter)) return false;
            _gfd.Characters.Remove(gfdCharacter);
            return true;
        }
    }
}
