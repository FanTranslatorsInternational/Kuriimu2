using Kanvas.Interface;
using Kanvas.Models;
using Kanvas.Quantization.Interfaces;
using Komponent.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Palette
{
    // TODO: Remove or merge later on; Only temporary
    public class Index2 : IIndexEncoding
    {
        private readonly int _indexDepth;
        private readonly IColorEncoding _paletteFormat;
        private readonly ByteOrder _byteOrder;
        private readonly IColorQuantizer _quantizer;

        // TODO: Make quantizer optional?
        public Index2(int indexDepth, IColorEncoding paletteFormat, IColorQuantizer quantizer, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            if (indexDepth % 8 != 0) throw new InvalidOperationException("IndexDepth needs to be dividable by 8.");

            _indexDepth = indexDepth;
            _paletteFormat = paletteFormat;
            _byteOrder = byteOrder;
            _quantizer = quantizer;
        }

        public (IEnumerable<IndexData> indeces, IList<Color> palette) Load(byte[] input, byte[] paletteData)
        {
            var palette = _paletteFormat.Load(paletteData).ToList();

            var result = new List<IndexData>();
            using (var br = new BinaryReaderX(new MemoryStream(input), false, _byteOrder))
                while (br.BaseStream.Position < br.BaseStream.Length || !br.IsFirstNibble)
                    switch (_indexDepth)
                    {
                        case 4:
                            result.Add(new IndexData(br.ReadNibble()));
                            break;
                        case 8:
                            result.Add(new IndexData(br.ReadByte()));
                            break;
                        default:
                            throw new InvalidOperationException($"IndexDepth {_indexDepth} not supported.");
                    }

            return (result, palette);
        }

        public IEnumerable<Color> Compose(IEnumerable<IndexData> indices, IList<Color> palette)
        {
            foreach (var indexData in indices)
                yield return palette[indexData.Index];
        }

        public (IEnumerable<IndexData> indeces, IList<Color> palette) Decompose(IEnumerable<Color> colors)
        {
            var palette = new ConcurrentDictionary<int, Color>();
            var indeces = new List<IndexData>();

            foreach (var c in colors)
            {
                var colorKey = c.ToArgb();

                palette.AddOrUpdate(colorKey, key => c, (key, color) => c);
                var index = palette.Keys.ToList().IndexOf(colorKey);
                indeces.Add(new IndexData(index));
            }

            return (indeces, palette.Values.ToList());
        }

        public (IEnumerable<IndexData> indeces, IList<Color> palette) Quantize(IEnumerable<Color> colors)
        {
            var data = _quantizer.Process(colors);
            return (data.Select(i => new IndexData(i)), _quantizer.GetPalette());
        }

        public (byte[] indexData, byte[] paletteData) Save(IEnumerable<IndexData> indeces, IList<Color> palette)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true, _byteOrder))
            {
                foreach (var indexData in indeces)
                    switch (_indexDepth)
                    {
                        case 4:
                            bw.WriteNibble(indexData.Index);
                            break;
                        case 8:
                            bw.Write((byte)indexData.Index);
                            break;
                        default:
                            throw new Exception($"IndexDepth {_indexDepth} not supported!");
                    }
            }

            return (ms.ToArray(), _paletteFormat.Save(palette));
        }
    }
}
