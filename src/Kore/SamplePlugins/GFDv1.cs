using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Image;

namespace Kore.SamplePlugins
{
    public class GFDv1 : IFormatConverter<GFDv1, GFDv2>
    {
        public FileHeader Header;
        public List<float> HeaderF;
        public string Name;
        public List<GFDv1Character> Characters;
        public List<Bitmap> Textures;

        public ByteOrder ByteOrder = ByteOrder.LittleEndian;
        public BitOrder BitOrder = BitOrder.MSBFirst;

        private string _sourceFile;

        #region MT Texture Adapters

        private CompositionContainer _container;

        [ImportMany(typeof(IMtFrameworkTextureAdapter))]
        private List<IMtFrameworkTextureAdapter> _texAdapters;

        #endregion

        public GFDv1()
        {
            Header = new FileHeader();
            HeaderF = new List<float>();
            Name = string.Empty;
            Characters = new List<GFDv1Character>();
            Textures = new List<Bitmap>();

            //TODO
            //Plugins.ComposePlugins(this, _container);
            KoreManager.ComposeSamplePlugins(this, _container);
        }

        public GFDv1(FileStream input)
        {
            _sourceFile = input.Name;

            //TODO
            //Plugins.ComposePlugins(this, _container);
            KoreManager.ComposeSamplePlugins(this, _container);

            using (var br = new BinaryReaderX(input))
            {
                // Set endianess
                if (br.PeekString() == "\0DFG")
                {
                    br.ByteOrder = ByteOrder = ByteOrder.BigEndian;
                    br.BitOrder = BitOrder = BitOrder.LSBFirst;
                }

                // Header
                Header = br.ReadStruct<FileHeader>();
                HeaderF = br.ReadMultiple<float>(Header.FCount);

                // Name
                br.ReadInt32();
                Name = br.ReadCStringASCII();

                // Characters
                Characters = br.ReadMultiple<CharacterInfo>(Header.CharacterCount).Select(ci => new GFDv1Character
                {
                    Character = ci.Character,

                    GlyphX = (int)ci.Block1.GlyphX,
                    GlyphY = (int)ci.Block1.GlyphY,
                    TextureID = (int)ci.Block1.TextureIndex,

                    GlyphHeight = (int)ci.Block2.GlyphHeight,
                    GlyphWidth = (int)ci.Block2.GlyphWidth,
                    XAdjust = (int)ci.Block2.XAdjust,

                    IsSpace = ci.Block3.IsSpace > 0 ? true : false,
                    Superscript = ci.Block3.Superscript > 0 ? true : false,
                    CharacterWidth = (int)ci.Block3.CharacterWidth
                }).ToList();

                // Textures
                Textures = new List<Bitmap>();
                for (var i = 0; i < Header.FontTexCount; i++)
                {
                    //TODO
                    IMtFrameworkTextureAdapter texAdapter = null;//_texAdapters.Where(adapter => adapter is IIdentifyFiles).FirstOrDefault(adapter => ((IIdentifyFiles)adapter).Identify(GetTexName(_sourceFile, i)));
                    if (texAdapter == null) continue;
                    //TODO
                    //((ILoadFiles)texAdapter).Load(GetTexName(_sourceFile, i));
                    Textures.Add(((IImageAdapter)texAdapter).BitmapInfos[0].Image);
                }
            }
        }

        public void Save(FileStream output)
        {
            using (var bw = new BinaryWriterX(output, ByteOrder, BitOrder))
            {
                // Header
                Header.Magic = ByteOrder == ByteOrder.LittleEndian ? "GFD\0" : "\0DFG";
                Header.CharacterCount = Characters.Count;
                Header.FontTexCount = Textures.Count;
                bw.WriteStruct(Header);
                foreach (var f in HeaderF)
                    bw.Write(f);

                // Name
                bw.Write(Name.Length);
                bw.WriteString(Name, Encoding.ASCII, false, false);
                bw.Write((byte)0);

                // Characters
                bw.WriteMultiple(Characters.Select(ci => new CharacterInfo
                {
                    Character = ci.Character,

                    Block1 = new Block1
                    {
                        GlyphY = ci.GlyphY,
                        GlyphX = ci.GlyphX,
                        TextureIndex = ci.TextureID
                    },

                    Block2 = new Block2
                    {
                        GlyphHeight = ci.GlyphHeight,
                        GlyphWidth = ci.GlyphWidth,
                        XAdjust = ci.XAdjust
                    },

                    Block3 = new Block3
                    {
                        IsSpace = ci.IsSpace ? 1 : 0,
                        Superscript = ci.Superscript ? 1 : 0,
                        CharacterWidth = ci.CharacterWidth
                    }
                }));

                // Textures
                for (var i = 0; i < Header.FontTexCount; i++)
                {
                    //TODO
                    IMtFrameworkTextureAdapter texAdapter = null;//_texAdapters.Where(adapter => adapter is IIdentifyFiles).FirstOrDefault(adapter => ((IIdentifyFiles)adapter).Identify(GetTexName(_sourceFile, i)));
                    if (texAdapter == null) continue;
                    //TODO
                    //((ILoadFiles)texAdapter).Load(GetTexName(_sourceFile, i));
                    ((IImageAdapter)texAdapter).BitmapInfos[0].Image = Textures[i];
                    //((ISaveFiles)texAdapter).Save(GetTexName(output.Name, i));
                }

                _sourceFile = output.Name;
            }
        }

        private string GetTexName(string filename, int textureIndex)
        {
            var dName = Path.GetDirectoryName(filename);
            var fName = Name.Split('\\').Last() + "_" + textureIndex.ToString("00");

            switch (Header.Suffix)
            {
                case 0x1:
                    fName += "_ID";
                    break;
                case 0x3:
                    fName += "_ID_HQ";
                    break;
                case 0x6:
                    fName += "_AM_NOMIP";
                    break;
            }
            fName += ".tex";

            return Path.Combine(dName, fName);
        }

        // Conversion
        public GFDv2 ConvertTo(GFDv1 inFormat)
        {
            throw new System.NotImplementedException();
        }

        public static implicit operator GFDv2(GFDv1 source)
        {
            throw new System.NotImplementedException();
        }

        // Support
        public class FileHeader
        {
            [FixedLength(4)]
            public string Magic;
            public uint Version;

            /// <summary>
            /// IsDynamic, InsertSpace, EvenLayout
            /// </summary>
            public int HeaderBlock1;

            /// <summary>
            /// This is texture suffix id (as in NOMIP, etc.)
            /// 0x0 and anything greater than 0x6 means no suffix
            /// </summary>
            public int Suffix;

            public int FontType;
            public int FontSize;
            public int FontTexCount;
            public int CharacterCount;
            public int FCount;

            /// <summary>
            /// Internally called MaxAscent
            /// </summary>
            public float Baseline;

            /// <summary>
            /// Internally called MaxDescent
            /// </summary>
            public float DescentLine;
        }

        public class CharacterInfo
        {
            public uint Character;
            public Block1 Block1;
            public Block2 Block2;
            public Block3 Block3;
        }

        [BitFieldInfo(BlockSize = 32)]
        public struct Block1
        {
            [BitField(12)]
            public long GlyphY;
            [BitField(12)]
            public long GlyphX;
            [BitField(8)]
            public long TextureIndex;
        }

        [BitFieldInfo(BlockSize = 32)]
        public struct Block2
        {
            [BitField(12)]
            public long GlyphHeight;
            [BitField(12)]
            public long GlyphWidth;
            [BitField(8)]
            public long XAdjust;
        }

        [BitFieldInfo(BlockSize = 32)]
        public struct Block3
        {
            [BitField(14)]
            public long IsSpace;
            [BitField(6)]
            public long Superscript;
            [BitField(12)]
            public long CharacterWidth;
        }
    }

    public class GFDv1Character : FontCharacter
    {
        /// <summary>
        /// Character width.
        /// </summary>
        [FormField(typeof(int), "Character Width")]
        public int CharacterWidth { get; set; }

        /// <summary>
        /// Trailing 8 bits in block2 that are unknown
        /// </summary>
        [FormField(typeof(int), "X Adjust")]
        public int XAdjust { get; set; }

        /// <summary>
        /// Determines whether the character is a space.
        /// </summary>
        [FormField(typeof(bool), "Is Space")]
        public bool IsSpace { get; set; }

        /// <summary>
        /// Determines whether the character is superscript.
        /// </summary>
        [FormField(typeof(bool), "Superscript")]
        public bool Superscript { get; set; }

        /// <summary>
        /// Allows cloning of GfdCharcaters,
        /// </summary>
        /// <returns>A cloned GfdCharacter.</returns>
        public override object Clone() => new GFDv1Character
        {
            Character = Character,
            TextureID = TextureID,
            GlyphX = GlyphX,
            GlyphY = GlyphY,
            GlyphWidth = GlyphWidth,
            GlyphHeight = GlyphHeight,
            XAdjust = XAdjust,
            CharacterWidth = CharacterWidth,
            Superscript = Superscript,
            IsSpace = IsSpace
        };
    }
}
