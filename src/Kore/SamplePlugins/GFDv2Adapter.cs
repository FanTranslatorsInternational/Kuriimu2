using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;

namespace Kore.SamplePlugins
{
    [Export(typeof(GFDv1Adapter))]
    [Export(typeof(IFontAdapter))]
    [Export(typeof(IIdentifyFiles))]
    //[Export(typeof(ILoadFiles))]
    [Export(typeof(ISaveFiles))]
    [PluginInfo("7D5D83B2-9F25-4722-BB4B-5E2C1D07AA8D", "MT Framework Font v2", "GFDv2", "IcySon55", "", "This is the GFDv2 font adapter for Kuriimu.")]
    [PluginExtensionInfo("*.gfd")]
    public sealed class GFDv2Adapter : IFontAdapter, IIdentifyFiles, /*ILoadFiles,*/ ISaveFiles, IAddCharacters, IDeleteCharacters
    {
        private GFDv2 _gfd;

        public enum Versions : uint
        {
            _3DSv2 = 0x10F06, // 68614 GFDv2
        }

        #region Properties

        [FormFieldIgnore]
        public IEnumerable<FontCharacter> Characters
        {
            get => _gfd.Characters;
            set => _gfd.Characters = value.Select(fc => (GFDv2Character)fc).ToList();
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

        [FormField(typeof(float), "Max Character Width")]
        public float MaxCharWidth
        {
            get => _gfd.Header.MaxCharWidth;
            set => _gfd.Header.MaxCharWidth = value;
        }

        [FormField(typeof(float), "Max Character Height")]
        public float MaxCharHeight
        {
            get => _gfd.Header.MaxCharHeight;
            set => _gfd.Header.MaxCharHeight = value;
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
                        if (version != Versions._3DSv2)
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
        //    _gfd = new GFDv2();
        //}

        public void Load(string filename)
        {
            if (File.Exists(filename))
            {
                _gfd = new GFDv2(File.OpenRead(filename));
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
            var newChar = new GFDv2Character();

            if (selectedCharacter is GFDv2Character chrv2)
            {
                newChar.Block2Trailer = chrv2.Block2Trailer;
                newChar.CharacterWidth = chrv2.CharacterWidth;
                newChar.CharacterHeight = chrv2.CharacterHeight;
                newChar.XAdjust = chrv2.XAdjust;
                newChar.YAdjust = chrv2.YAdjust;
            }

            return newChar;
        }

        public bool AddCharacter(FontCharacter character)
        {
            if (!(character is GFDv2Character chr)) return false;
            _gfd.Characters.Add(chr);

            // Set Character Width and Height
            if (chr.CharacterWidth == 0)
                chr.CharacterWidth = chr.GlyphWidth - 1; // They often seem to subtract one
            if (chr.CharacterHeight == 0)
                chr.CharacterHeight = chr.GlyphHeight; // Use glyph height because we don't generate compact textures.

            _gfd.Characters.Sort((l, r) => l.Character.CompareTo(r.Character));

            return true;
        }

        public bool DeleteCharacter(FontCharacter character)
        {
            if (!(character is GFDv2Character chr)) return false;
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
