using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;

namespace Kore.XFont
{
    [Export(typeof(XfAdapter))]
    [Export(typeof(IFontAdapter))]
    //[Export(typeof(IIdentifyFiles))]
    //[Export(typeof(ILoadFiles))]
    //[Export(typeof(ISaveFiles))]
    [PluginInfo("33B42E7E-FFA6-4F8D-B30A-D0334910BC45", "Level 5 Font", "XF", "onepiecefreak", "", "This is the XF font adapter for Kuriimu.")]
    [PluginExtensionInfo("*.xf")]
    public sealed class XfAdapter : IFontAdapter, /*IIdentifyFiles, ILoadFiles,*/ IAddCharacters, IDeleteCharacters
    {
        private XF _xf;

        #region Properties

        public IEnumerable<FontCharacter> Characters
        {
            get
            {
                return _xf.Characters;
            }
            set
            {
                _xf.Characters = value.Select(fc => (XFCharacter)fc).ToList();
            }
        }

        public List<Bitmap> Textures { get; set; }

        public float Baseline
        {
            get
            {
                return _xf.Header.BaseLine;
            }
            set
            {
                _xf.Header.BaseLine = (int)value;
            }
        }

        public float DescentLine
        {
            get
            {
                return _xf.Header.DescentLine;
            }
            set
            {
                _xf.Header.DescentLine = (short)value;
            }
        }

        #endregion

        public bool Identify(string filename)
        {
            using (var br = new BinaryReaderX(File.OpenRead(filename)))
            {
                var magic = br.ReadString(4, Encoding.ASCII);
                return magic == "XPCK";
            }
        }

        public void Load(string filename)
        {
            if (File.Exists(filename))
            {
                _xf = new XF(File.OpenRead(filename));

                // Load Textures
                Textures = new List<Bitmap>();
                Textures.AddRange(_xf.Textures);
            }
            else
                throw new FileNotFoundException();
        }

        public void Save(string filename, int versionIndex = 0)
        {
            _xf.Save(File.OpenWrite(filename));
        }

        public FontCharacter NewCharacter(FontCharacter selectedCharacter = null)
        {
            return null;
            /*FontCharacter newChar = null;

            switch (_version)
            {
                case 1:
                    newChar = new GFDv1Character();
                    if (selectedCharacter is GFDv1Character chrv1)
                    {
                        var chr = (GFDv1Character)newChar;
                        chr.Block2Trailer = chrv1.Block2Trailer;
                        chr.CharacterKerning = chrv1.CharacterKerning;
                        chr.CharacterUnknown = chrv1.CharacterUnknown;
                        chr.Block3Trailer = chrv1.Block3Trailer;
                    }
                    break;
                case 2:
                    newChar = new GFDv2Character();
                    if (selectedCharacter is GFDv2Character chrv2)
                    {
                        var chr = (GFDv2Character)newChar;
                        chr.Block2Trailer = chrv2.Block2Trailer;
                        chr.CharacterWidth = chrv2.CharacterWidth;
                        chr.CharacterHeight = chrv2.CharacterHeight;
                        chr.XAdjust = chrv2.XAdjust;
                        chr.YAdjust = chrv2.YAdjust;
                    }
                    break;
            }

            return newChar;*/
        }

        public bool AddCharacter(FontCharacter character)
        {
            return false;
            /*switch (_version)
            {
                case 1:
                    if (!(character is GFDv1Character chrv1)) return false;
                    _gfdv1.Characters.Add(chrv1);

                    // Set GFD Kerning for new characters
                    if (chrv1.CharacterKerning == 0)
                        chrv1.CharacterKerning = chrv1.GlyphWidth;

                    // Set GFD Unknown for space characters
                    switch (character.Character)
                    {
                        case 'Ø':
                        case '¬':
                        case 'þ':
                        case ' ':
                            chrv1.CharacterUnknown = 32;
                            break;
                    }

                    _gfdv1.Characters.Sort((l, r) => l.Character.CompareTo(r.Character));
                    break;
                case 2:
                    if (!(character is GFDv2Character chrv2)) return false;
                    _gfdv2.Characters.Add(chrv2);

                    // Set Character Width and Height
                    if (chrv2.CharacterWidth == 0)
                        chrv2.CharacterWidth = chrv2.GlyphWidth - 1; // They often seem to subtract one
                    if (chrv2.CharacterHeight == 0)
                        chrv2.CharacterHeight = chrv2.GlyphHeight; // Use glyph height because we don't generate compact textures.

                    _gfdv2.Characters.Sort((l, r) => l.Character.CompareTo(r.Character));
                    break;
            }

            return true;*/
        }

        public bool DeleteCharacter(FontCharacter character)
        {
            return false;
            /*switch (_version)
            {
                case 1:
                    if (!(character is GFDv1Character chrv1)) return false;
                    _gfdv1.Characters.Remove(chrv1);
                    break;
                case 2:
                    if (!(character is GFDv2Character chrv2)) return false;
                    _gfdv2.Characters.Remove(chrv2);
                    break;
            }

            return true;*/
        }

        public void Dispose()
        {
            foreach (var tex in Textures)
                tex.Dispose();
            _xf = null;
        }
    }
}
