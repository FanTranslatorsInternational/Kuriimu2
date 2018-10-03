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
    [Export(typeof(GFDv1Adapter))]
    [Export(typeof(IFontAdapter))]
    [Export(typeof(IIdentifyFiles))]
    [Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("3C8827B8-D124-45D7-BD4C-2A98E049A20A", "MT Framework Font v1", "GFDv1", "IcySon55", "", "This is the GFDv1 font adapter for Kuriimu.")]
    [PluginExtensionInfo("*.gfd")]
    public sealed class GFDv1Adapter : IFontAdapter, IIdentifyFiles, ILoadFiles, ISaveFiles, IAddCharacters, IDeleteCharacters
    {
        private GFDv1 _gfd;

        public enum Versions : uint
        {
            _3DSv1 = 0x10A05, // 68101 GFDv1
            _3DSv2 = 0x10C06, // 68614 GFDv1
            _PS3v1 = 0x10B05, // 68357 GFDv1
        }

        #region Properties

        [FormFieldIgnore]
        public IEnumerable<FontCharacter> Characters
        {
            get => _gfd.Characters;
            set => _gfd.Characters = value.Select(fc => (GFDv1Character)fc).ToList();
        }

        [FormFieldIgnore]
        public List<Bitmap> Textures
        {
            get => _gfd.Textures;
            set => _gfd.Textures = value;
        }

        [FormField(typeof(int), "Font Size")]
        public int FontSize
        {
            get => _gfd.Header.FontSize;
            set => _gfd.Header.FontSize = value;
        }

        [FormField(typeof(float), "Base Line")]
        public float Baseline
        {
            get => _gfd.Header.Baseline;
            set => _gfd.Header.Baseline = value;
        }

        [FormField(typeof(float), "Descent Line")]
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
                    if (br.BaseStream.Length < 8)
                        result = false;

                    if (result)
                    {
                        if (br.PeekString() == "\0DFG")
                        {
                            br.ByteOrder = ByteOrder.BigEndian;
                            br.BitOrder = BitOrder.LSBFirst;
                        }

                        var magic = br.ReadString(4);
                        if (!magic.StartsWith("GFD\0") && !magic.StartsWith("\0DFG"))
                            result = false;

                        var version = (Versions)br.ReadUInt32();
                        if (version != Versions._3DSv1 && version != Versions._3DSv2 && version != Versions._PS3v1)
                            result = false;
                    }
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
        //}

        public void Load(string filename)
        {
            if (File.Exists(filename))
            {
                _gfd = new GFDv1(File.OpenRead(filename));
            }
            else
                throw new FileNotFoundException();
        }

        public void Save(string filename, int versionIndex = 0)
        {
            _gfd.Save(File.Create(filename));
        }

        public FontCharacter NewCharacter(FontCharacter selectedCharacter = null)
        {
            var newChar = new GFDv1Character();

            if (selectedCharacter is GFDv1Character chr)
            {
                newChar.XAdjust = chr.XAdjust;
                newChar.CharacterWidth = chr.CharacterWidth;
                newChar.Superscript = chr.Superscript;
                newChar.IsSpace = chr.IsSpace;
            }

            return newChar;
        }

        public bool AddCharacter(FontCharacter character)
        {
            if (!(character is GFDv1Character chr)) return false;
            _gfd.Characters.Add(chr);

            // Set GFD Character Width for new characters
            if (chr.CharacterWidth == 0)
                chr.CharacterWidth = chr.GlyphWidth;

            _gfd.Characters.Sort((l, r) => l.Character.CompareTo(r.Character));

            return true;
        }

        public bool DeleteCharacter(FontCharacter character)
        {
            if (!(character is GFDv1Character chr)) return false;
            _gfd.Characters.Remove(chr);

            return true;
        }

        public void Dispose()
        {
            foreach (var tex in Textures)
                tex.Dispose();
        }
    }
}
