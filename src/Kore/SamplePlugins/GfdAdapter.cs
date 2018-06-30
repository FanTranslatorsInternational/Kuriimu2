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
        private GFDv1 _gfdv1;
        private GFDv2 _gfdv2;
        private int _version = 1;

        public enum Version : uint
        {
            _3DSv1 = 0x10A05, // 68101 GFDv1
            _3DSv2 = 0x10F06, // 69382 GFDv2
            _PS3v1 = 0x10B05, // 68357 GFDv1
        }

        #region Properties

        [FormFieldIgnore]
        public IEnumerable<FontCharacter> Characters
        {
            get
            {
                switch (_version)
                {
                    case 1:
                        return _gfdv1.Characters;
                    case 2:
                        return _gfdv2.Characters;
                    default:
                        return null;
                }
            }
            set
            {
                switch (_version)
                {
                    case 1:
                        _gfdv1.Characters = value.Select(fc => (GFDv1Character)fc).ToList();
                        break;
                    case 2:
                        _gfdv2.Characters = value.Select(fc => (GFDv2Character)fc).ToList();
                        break;
                }
            }
        }

        [FormFieldIgnore]
        public List<Bitmap> Textures { get; set; }

        [FormField(typeof(int), "Font Size")]
        public int FontSize
        {
            get
            {
                switch (_version)
                {
                    case 1:
                        return _gfdv1.Header.FontSize;
                    case 2:
                        return _gfdv2.Header.FontSize;
                    default:
                        return 0;
                }
            }
            set
            {
                switch (_version)
                {
                    case 1:
                        _gfdv1.Header.FontSize = value;
                        break;
                    case 2:
                        _gfdv2.Header.FontSize = value;
                        break;
                }
            }
        }

        [FormField(typeof(float), "(GFDv2) Max Character Width")]
        public float MaxCharWidth
        {
            get
            {
                switch (_version)
                {
                    case 2:
                        return _gfdv2.Header.MaxCharWidth;
                    default:
                        return 0;
                }
            }
            set
            {
                switch (_version)
                {
                    case 2:
                        _gfdv2.Header.MaxCharWidth = value;
                        break;
                }
            }
        }

        [FormField(typeof(float), "(GFDv2) Max Character Height")]
        public float MaxCharHeight
        {
            get
            {
                switch (_version)
                {
                    case 2:
                        return _gfdv2.Header.MaxCharHeight;
                    default:
                        return 0;
                }
            }
            set
            {
                switch (_version)
                {
                    case 2:
                        _gfdv2.Header.MaxCharHeight = value;
                        break;
                }
            }
        }

        [FormField(typeof(float), "Base Line")]
        public float Baseline
        {
            get
            {
                switch (_version)
                {
                    case 1:
                        return _gfdv1.Header.Baseline;
                    case 2:
                        return _gfdv2.Header.Baseline;
                    default:
                        return 0;
                }
            }
            set
            {
                switch (_version)
                {
                    case 1:
                        _gfdv1.Header.Baseline = value;
                        break;
                    case 2:
                        _gfdv2.Header.Baseline = value;
                        break;
                }
            }
        }

        [FormField(typeof(float), "Descent Line")]
        public float DescentLine
        {
            get
            {
                switch (_version)
                {
                    case 1:
                        return _gfdv1.Header.DescentLine;
                    case 2:
                        return _gfdv2.Header.DescentLine;
                    default:
                        return 0;
                }
            }
            set
            {
                switch (_version)
                {
                    case 1:
                        _gfdv1.Header.DescentLine = value;
                        break;
                    case 2:
                        _gfdv2.Header.DescentLine = value;
                        break;
                }
            }
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

        //public void Create()
        //{
        //    _gfdv1 = new GFDv1();
        //    Textures = new List<Bitmap>();
        //}

        public void Load(string filename)
        {
            if (File.Exists(filename))
            {
                // Determine Version
                using (var br = new BinaryReaderX(File.OpenRead(filename)))
                {
                    br.BaseStream.Position += sizeof(int);
                    switch ((Version)br.ReadUInt32())
                    {
                        case Version._3DSv1:
                        case Version._PS3v1:
                            _version = 1;
                            break;
                        case Version._3DSv2:
                            _version = 2;
                            break;
                        default:
                            _version = 1;
                            break;
                    }
                }

                switch (_version)
                {
                    case 1:
                        _gfdv1 = new GFDv1(File.OpenRead(filename));
                        break;
                    case 2:
                        _gfdv2 = new GFDv2(File.OpenRead(filename));
                        break;
                }

                // Load Textures
                var textureFiles = Directory.GetFiles(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "*.png");

                Textures = new List<Bitmap>();
                foreach (var file in textureFiles)
                    Textures.Add((Bitmap)Image.FromFile(file));
            }
            else
                throw new FileNotFoundException();
        }

        public void Save(string filename, int versionIndex = 0)
        {
            switch (_version)
            {
                case 1:
                    _gfdv1.Header.FontTexCount = Textures.Count;
                    _gfdv1.Save(File.Create(filename));
                    break;
                case 2:
                    _gfdv2.Header.FontTexCount = Textures.Count;
                    _gfdv2.Save(File.Create(filename));
                    break;
            }
            //for (var i = 0; i < Textures.Count; i++)
            //{
            //    var tex = Textures[i];
            //    tex.Save(Path.Combine(Path.GetDirectoryName(filename), $"{Path.GetFileNameWithoutExtension(filename)}_{i:00}.png"), ImageFormat.Png);
            //}
        }

        public FontCharacter NewCharacter(FontCharacter selectedCharacter = null)
        {
            FontCharacter newChar = null;

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

            return newChar;
        }

        public bool AddCharacter(FontCharacter character)
        {
            switch (_version)
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

            return true;
        }

        public bool DeleteCharacter(FontCharacter character)
        {
            switch (_version)
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

            return true;
        }

        public void Dispose()
        {
            foreach (var tex in Textures)
                tex.Dispose();
        }
    }
}
