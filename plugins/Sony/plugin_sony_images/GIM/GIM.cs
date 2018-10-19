using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Komponent.IO;

namespace plugin_sony_images.GIM
{
    public sealed class GIM
    {
        public List<List<Bitmap>> Images { get; set; }

        public GIM(Stream input)
        {
            using (var br = new BinaryReaderX(input))
            {
                var magic = br.ReadBytes(0x10);
                var root = new RootBlock(input);

                Images = root.PictureBlocks.SelectMany(p => p.ImageBlocks.Select(i => i.Bitmaps)).ToList();

                //var migChunks = new List<GIMChunk>();
                //var magic = br.ReadBytes(0x10);
                //// MIG consists of chunks too, without magic, but chunk indices instead
                //// Chunk index 4 contains image data, chunk index 5 the palette

                //while (br.BaseStream.Position < br.BaseStream.Length)
                //{
                //    migChunks.Add(new MIGChunk
                //    {
                //        ID = br.ReadInt32(),
                //        ChildChunkOffset = br.ReadInt32(),
                //        ChunkSize = br.ReadInt32(),
                //        ChunkHeaderSize = br.ReadInt32()
                //    });
                //    migChunks.Last().Data = br.ReadBytes(migChunks.Last().ChunkSize - migChunks.Last().ChunkHeaderSize);
                //}

                //// Get Palette
                //var colors = new List<Color>();
                //using (var paletteBr = new BinaryReaderX(new MemoryStream(migChunks.First(c => c.ID == 5).Data)))
                //{
                //    paletteBr.BaseStream.Position += 0x30;

                //    var paletteSize = paletteBr.ReadInt32();
                //    paletteBr.BaseStream.Position += 0xC;

                //    while (paletteBr.BaseStream.Position < paletteBr.BaseStream.Length)
                //    {
                //        var r = paletteBr.ReadByte();
                //        var g = paletteBr.ReadByte();
                //        var b = paletteBr.ReadByte();
                //        var a = paletteBr.ReadByte();
                //        colors.Add(Color.FromArgb(a, r, g, b));
                //    }
                //}

                //// Get List of Pixels
                //var width = 0;
                //var height = 0;
                //var imgList = new List<Color>();
                //using (var indexBr = new BinaryReader(new MemoryStream(migChunks.First(c => c.ID == 4).Data)))
                //{
                //    indexBr.BaseStream.Position += 0x8;
                //    width = indexBr.ReadInt16();
                //    height = indexBr.ReadInt16();
                //    indexBr.BaseStream.Position += 0x34;
                //    switch (colors.Count)
                //    {
                //        case 16:
                //            while (indexBr.BaseStream.Position < indexBr.BaseStream.Length)
                //            {
                //                var index = indexBr.ReadByte();
                //                imgList.Add(colors[index & 0xF]);
                //                imgList.Add(colors[index >> 4]);
                //            }
                //            break;
                //        case 256:
                //            while (indexBr.BaseStream.Position < indexBr.BaseStream.Length)
                //            {
                //                var index = indexBr.ReadByte();
                //                imgList.Add(colors[index]);
                //            }
                //            break;
                //        default:
                //            imgList.Add(Color.Black);
                //            break;
                //    }
                //}

                //// Build Image
                //Image = new Bitmap(width, height);
                //for (var i = 0; i < imgList.Count; i++)
                //    Image.SetPixel(i % width, i / width, imgList[i]);
            }
        }
    }
}
