using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Komponent.IO;

namespace plugin_valkyria_chronicles.Game
{
    /// <summary>
    /// The SLICE format class handles image slicing data in a simple binary format.
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
            public string Name; // Slice name, no spaces recommended.
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