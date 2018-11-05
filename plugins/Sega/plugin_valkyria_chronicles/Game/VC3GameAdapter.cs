using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using Komponent.IO;
using Kontract.Attributes;
using Kontract.Interfaces;
using plugin_valkyria_chronicles.SFNT;

namespace plugin_valkyria_chronicles.Game
{
    [Export(typeof(VC3GameAdapter))]
    [Export(typeof(IGameAdapter))]
    [Export(typeof(IGenerateGamePreviews))]
    [PluginInfo("84D2BD62-7AC6-459B-B3BB-3A65855135F6", "Valkyria Chronicles 3", "VC3GA", "IcySon55")]
    public sealed class VC3GameAdapter : IGameAdapter, IGenerateGamePreviews
    {
        private string BasePath => Path.Combine("plugins", ID);

        private static readonly Lazy<SfntFontAdapter> FontInitializer = new Lazy<SfntFontAdapter>(() => new SfntFontAdapter());
        private SfntFontAdapter Font
        {
            get
            {
                if (!FontInitializer.IsValueCreated)
                {
                    var fontPath = Path.Combine(BasePath, "ODIN_FONT_16.BF1");
                    if (File.Exists(fontPath))
                        FontInitializer.Value.Load(fontPath);
                }
                return FontInitializer.Value;
            }
        }

        public string ID => ((PluginInfoAttribute)typeof(VC3GameAdapter).GetCustomAttribute(typeof(PluginInfoAttribute))).ID;

        public string Name => ((PluginInfoAttribute)typeof(VC3GameAdapter).GetCustomAttribute(typeof(PluginInfoAttribute))).Name;

        public string IconPath => Path.Combine("plugins", ID, "icon.png");

        public string Filename { get; set; }

        public IEnumerable<TextEntry> Entries { get; private set; }

        public void LoadEntries(IEnumerable<TextEntry> entries)
        {
            Entries = entries;
        }

        public IEnumerable<TextEntry> SaveEntries()
        {
            return Entries;
        }

        public Bitmap GeneratePreview(TextEntry entry)
        {
            if (!Font.Characters.Any()) return null;

            // Paths
            var bg01Path = Path.Combine(BasePath, "BG01.HTX.png");

            // Setup
            var BG01 = new Bitmap(bg01Path);

            var img = new Bitmap(BG01.Width, BG01.Height, PixelFormat.Format32bppArgb);

            // Colors
            var cDefault = Color.FromArgb(90, 47, 22);
            var cNumber = Color.FromArgb(64, 42, 19);

            var gfx = Graphics.FromImage(img);
            gfx.SmoothingMode = SmoothingMode.None;
            gfx.InterpolationMode = InterpolationMode.NearestNeighbor;
            gfx.PixelOffsetMode = PixelOffsetMode.None;

            // Load Files
            var balloonLokiHard = new Dicer(Path.Combine(BasePath, "BALLOON_LOKI_HARD.HTX.png"));

            // Scene Variables
            var scene = 1; // Make an enum
            int x = 0, y = 0, xR = 0;
            float scaleX = 0.75f, scaleY = 0.75f;
            var lineHeight = Font.Characters.First().GlyphHeight;

            // Begin Drawing
            switch (scene)
            {
                case 1:
                    Font.SetColor(cDefault);

                    gfx.DrawImage(BG01, 0, 0);

                    var grid = balloonLokiHard.Slice.UserInt1;
                    var balloonStartX = 16;
                    balloonLokiHard.DrawSlice("BigCorner", gfx, balloonStartX, balloonStartX);
                    balloonLokiHard.DrawSlice("BigCorner", gfx, balloonStartX + grid * 14, balloonStartX, true);

                    balloonLokiHard.DrawSlice("Edge", gfx, balloonStartX + grid * 2, balloonStartX, false, true);
                    balloonLokiHard.DrawSlice("Edge", gfx, balloonStartX + grid * 2, balloonStartX + grid * 5);
                    balloonLokiHard.DrawSlice("Edge", gfx, balloonStartX + grid * 6, balloonStartX, false, true);
                    balloonLokiHard.DrawSlice("Edge", gfx, balloonStartX + grid * 6, balloonStartX + grid * 5);
                    balloonLokiHard.DrawSlice("Edge", gfx, balloonStartX + grid * 10, balloonStartX, false, true);
                    balloonLokiHard.DrawSlice("Edge", gfx, balloonStartX + grid * 10, balloonStartX + grid * 5);

                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 2, balloonStartX + grid);
                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 2, balloonStartX + grid * 2);
                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 2, balloonStartX + grid * 3);
                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 2, balloonStartX + grid * 4);
                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 6, balloonStartX + grid);
                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 6, balloonStartX + grid * 2);
                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 6, balloonStartX + grid * 3);
                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 6, balloonStartX + grid * 4);
                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 10, balloonStartX + grid);
                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 10, balloonStartX + grid * 2);
                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 10, balloonStartX + grid * 3);
                    balloonLokiHard.DrawSlice("Fill", gfx, balloonStartX + grid * 10, balloonStartX + grid * 4);

                    // Set text box
                    x = xR = balloonStartX + grid * 2;
                    y = balloonStartX + grid + 1;
                    scaleX = scaleY = 0.875f;

                    break;
            }
            
            gfx.SmoothingMode = SmoothingMode.HighQuality;
            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

            foreach (var c in entry.EditedText)
            {
                // Handle line break
                if (c == '\n')
                {
                    x = xR;
                    y += lineHeight;
                    continue;
                }

                if (c >= '0' && c <= '9')
                    Font.SetColor(cNumber);
                else
                    Font.SetColor(cDefault);

                Font.Draw(c, gfx, x, y, scaleX, scaleY);
                x += (int)(Font.GetCharWidthInfo(c).GlyphWidth * scaleX);
            }

            return img;
        }
    }

    internal sealed class Dicer
    {
        public Bitmap Bitmap { get; }
        public SLICE Slice { get; }

        public Dicer(string imagePath)
        {
            Bitmap = new Bitmap(imagePath);
            Slice = new SLICE(File.OpenRead(Path.ChangeExtension(imagePath, ".slice")));
        }

        public void DrawSlice(string sliceName, Graphics gfx, int x, int y, bool flipX = false, bool flipY = false)
        {
            var slice = Slice.Slices.FirstOrDefault(s => s.Name == sliceName);
            if (slice == null) return;
            gfx.DrawImage(Bitmap, new[] {
                    new Point(x + (flipX ? slice.Width : 0), y + (flipY ? slice.Height : 0)),
                    new Point(x + (flipX ? 0 : slice.Width), y + (flipY ? slice.Height : 0)),
                    new Point(x + (flipX ? slice.Width : 0), y + (flipY ? 0 : slice.Height))
                },
                new Rectangle(slice.X - (flipX ? 1 : 0), slice.Y, slice.Width, slice.Height),
                GraphicsUnit.Pixel
            );
        }
    }

    /// <summary>
    /// The SLICE format class handles image slicing data in a simple format.
    /// </summary>
    internal sealed class SLICE
    {
        private FileHeader Header { get; }

        public List<Slice> Slices { get; set; }

        public int UserInt1
        {
            get => Header.UserInt1;
            set => Header.UserInt1 = value;
        }

        public int UserInt2
        {
            get => Header.UserInt2;
            set => Header.UserInt2 = value;
        }

        public SLICE()
        {
            Header = new FileHeader();
            Slices = new List<Slice>();
        }

        public SLICE(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                Header = br.ReadStruct<FileHeader>();
                if (Header.FileSize != br.BaseStream.Length)
                    return;

                Slices = new List<Slice>();
                for (var i = 0; i < Header.SliceCount; i++)
                {
                    Slices.Add(new Slice
                    {
                        Name = br.ReadString(Header.NameSize).Trim('\0'),
                        X = br.ReadInt32(),
                        Y = br.ReadInt32(),
                        DX = br.ReadInt32(),
                        DY = br.ReadInt32()
                    });
                }
            }
        }

        public void Save(Stream output)
        {
            throw new NotImplementedException();
        }

        internal class FileHeader
        {
            [FixedLength(8)]
            public string Magic = "SLICEv1";
            [FixedLength(4)]
            public string FTI = "FTI";
            public int FileSize = 0;
            public int NameSize = 0x10; // Aligned to 8 bytes including null byte; Applies to all slices.
            public int SliceCount = 0;
            public int UserInt1 = 0;
            public int UserInt2 = 0;
        }

        internal class Slice
        {
            public string Name; // Slice name, no spaces.
            public int X;
            public int Y;
            public int DX;
            public int DY;

            public int Width => DX - X;
            public int Height => DY - Y;

            public Point XY => new Point(X, Y);
            public Point DXDY => new Point(DX, DY);
            public Rectangle Rect => new Rectangle(X, Y, Width, Height);

            public override string ToString() => Name.Trim('\0');
        }
    }
}
