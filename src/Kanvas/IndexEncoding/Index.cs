using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas.IndexEncoding.Models;
using Kanvas.Interface;
using Kanvas.Models;

namespace Kanvas.IndexEncoding
{
    // TODO: Remove or merge later on; Only temporary
    /// <summary>
    /// Defines the default index-only encoding.
    /// </summary>
    public class Index : IIndexEncoding
    {
        private readonly int _indexDepth;
        private readonly bool _allowAlpha;

        /// <inheritdoc cref="IIndexEncoding.FormatName"/>
        public string FormatName { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="Index"/>.
        /// </summary>
        /// <param name="indexDepth">Depth of the index component.</param>
        /// <param name="allowAlpha">If decomposition should allow alpha in distinction.</param>
        public Index(int indexDepth, bool allowAlpha)
        {
            if (!IsPowerOfTwo(indexDepth))
                throw new InvalidOperationException("Index depth needs to be a power of 2.");

            _allowAlpha = allowAlpha;
            _indexDepth = indexDepth;

            UpdateName();
        }

        private bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        private void UpdateName()
        {
            FormatName = $"I{_indexDepth}";
        }

        /// <inheritdoc cref="IIndexEncoding.Load(byte[])"/>
        public IEnumerable<IndexData> Load(byte[] input)
        {
            var mask = 1 << _indexDepth - 1;

            using (var br = new BinaryReader(new MemoryStream(input)))
                while (br.BaseStream.Position < br.BaseStream.Length)
                    switch (_indexDepth)
                    {
                        case 1:
                        case 2:
                        case 4:
                            var value4 = br.ReadByte();
                            for (int i = 8 / _indexDepth - 1; i >= 0; i--)
                                yield return new IndexData((value4 >> i * _indexDepth) & mask);
                            break;
                        case 8:
                            yield return new IndexData(br.ReadByte());
                            break;
                        default:
                            throw new InvalidOperationException($"IndexDepth {_indexDepth} not supported.");
                    }
        }

        /// <inheritdoc cref="IIndexEncoding.Compose(IEnumerable{IndexData},IList{Color})"/>
        public IEnumerable<Color> Compose(IEnumerable<IndexData> indices, IList<Color> palette)
            => indices.Select(indexData => indexData.Index < 0 || indexData.Index > palette.Count ? Color.Empty : palette[indexData.Index]);

        /// <inheritdoc cref="IIndexEncoding.Decompose(IEnumerable{Color})"/>
        public (IEnumerable<IndexData> indices, IList<Color> palette) Decompose(IEnumerable<Color> colors)
        {
            var palette = new ConcurrentDictionary<int, Color>();
            var indices = new List<IndexData>();

            foreach (var c in colors)
            {
                var colorKey = c.ToArgb();
                if (!_allowAlpha)
                    colorKey &= 0x00FFFFFF;

                palette.AddOrUpdate(colorKey, key => c, (key, color) => c);
                var index = palette.Keys.ToList().IndexOf(colorKey);

                indices.Add(new IndexData(index));
            }

            return (indices, palette.Values.ToList());
        }

        /// <inheritdoc cref="IIndexEncoding.DecomposeWithPalette(IEnumerable{Color},IList{Color})"/>
        public IEnumerable<IndexData> DecomposeWithPalette(IEnumerable<Color> colors, IList<Color> palette)
        {
            var paletteKeys = palette.Select(c => (int)(c.ToArgb() & (!_allowAlpha ? 0x00FFFFFF : 0xFFFFFFFF))).ToList();
            foreach (var c in colors)
            {
                var colorKey = c.ToArgb();
                if (!_allowAlpha)
                    colorKey &= 0x00FFFFFF;

                var index = paletteKeys.IndexOf(colorKey);
                if (index < 0)
                    Debugger.Break();

                yield return new IndexData(index);
            }
        }

        /// <inheritdoc cref="IIndexEncoding.Quantize(IEnumerable{Color},QuantizationSettings)"/>
        public (IEnumerable<IndexData> indices, IList<Color> palette) Quantize(IEnumerable<Color> colors, QuantizationSettings settings)
        {
            var (indices, palette) = Kolors.Quantize(colors, settings);

            return (indices.Select(x => new IndexData(x)), palette);
        }

        /// <inheritdoc cref="IIndexEncoding.Save(IEnumerable{IndexData})"/>
        public byte[] Save(IEnumerable<IndexData> indices)
        {
            byte valueBuffer = 0;
            var counter = 0;
            var mask = 1 << _indexDepth - 1;

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.ASCII, true))
            {
                foreach (var indexData in indices)
                    switch (_indexDepth)
                    {
                        case 1:
                        case 2:
                        case 4:
                            counter += _indexDepth;
                            valueBuffer <<= _indexDepth;
                            valueBuffer |= (byte)(indexData.Index & mask);

                            if (counter == 8)
                            {
                                bw.Write(valueBuffer);
                                counter = 0;
                                valueBuffer = 0;
                            }
                            break;
                        case 8:
                            bw.Write((byte)indexData.Index);
                            break;
                        default:
                            throw new Exception($"IndexDepth {_indexDepth} not supported!");
                    }

                if (counter > 0)
                    bw.Write((byte)(valueBuffer << (8 - counter)));
            }

            return ms.ToArray();
        }
    }
}
