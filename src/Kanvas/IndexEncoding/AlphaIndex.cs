using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas.IndexEncoding.Models;
using Kanvas.Interface;
using Kanvas.Models;
using Kanvas.Quantization.Models.ColorCache;
using Convert = Kanvas.Support.Convert;

namespace Kanvas.IndexEncoding
{
    /// <summary>
    /// Defines the default index-only encoding.
    /// </summary>
    public class AlphaIndex : IIndexEncoding
    {
        private readonly int _alphaDepth;
        private readonly int _indexDepth;
        private readonly int _bitDepth;

        /// <inheritdoc cref="IIndexEncoding.FormatName"/>
        public string FormatName { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="AlphaIndex"/>.
        /// </summary>
        /// <param name="alphaDepth">The bit depth of the alpha component.</param>
        /// <param name="indexDepth">The bit depth of the index component.</param>
        public AlphaIndex(int alphaDepth, int indexDepth)
        {
            if (indexDepth <= 0) throw new ArgumentOutOfRangeException(nameof(indexDepth));
            if (alphaDepth <= 0) throw new ArgumentOutOfRangeException(nameof(alphaDepth));
            //if ((alphaDepth + indexDepth) % 8 != 0) throw new InvalidOperationException("_alphaDepth + _indexDepth has to be dividable by 8.");

            _alphaDepth = alphaDepth;
            _indexDepth = indexDepth;
            _bitDepth = alphaDepth + indexDepth;

            UpdateName();
        }

        private void UpdateName()
        {
            FormatName = $"A{_alphaDepth}I{_indexDepth}";
        }

        /// <inheritdoc cref="IIndexEncoding.Load(byte[])"/>
        public IEnumerable<IndexData> Load(byte[] input)
        {
            var alphaShift = _indexDepth;
            var indexBitMask = (1 << _indexDepth) - 1;

            using (var br = new BinaryReader(new MemoryStream(input)))
                while (br.BaseStream.Position < br.BaseStream.Length)
                    switch (_bitDepth)
                    {
                        case 8:
                            var value = br.ReadByte();
                            yield return new AlphaIndexData(
                                Convert.ChangeBitDepth(value >> alphaShift, _alphaDepth, 8),
                                value & indexBitMask);
                            break;
                        default:
                            throw new InvalidOperationException($"IndexDepth {_indexDepth} not supported.");
                    }
        }

        /// <inheritdoc cref="IIndexEncoding.Compose(IEnumerable{IndexData},IList{Color})"/>
        public IEnumerable<Color> Compose(IEnumerable<IndexData> indices, IList<Color> palette)
        {
            foreach (var indexData in indices)
            {
                var alphaIndexData = (AlphaIndexData)indexData;
                var color = palette[alphaIndexData.Index];
                yield return Color.FromArgb(alphaIndexData.Alpha, color.R, color.G, color.B);
            }
        }

        /// <inheritdoc cref="IIndexEncoding.Decompose(IEnumerable{Color})"/>
        public (IEnumerable<IndexData> indices, IList<Color> palette) Decompose(IEnumerable<Color> colors)
        {
            var palette = new ConcurrentDictionary<int, Color>();
            var indices = new List<IndexData>();

            foreach (var c in colors)
            {
                var colorKey = c.ToArgb() & 0x00FFFFFF;

                palette.AddOrUpdate(colorKey, key => c, (key, color) => c);
                var index = palette.Keys.ToList().IndexOf(colorKey);

                indices.Add(new AlphaIndexData(c.A, index));
            }

            return (indices, palette.Values.ToList());
        }

        /// <inheritdoc cref="IIndexEncoding.DecomposeWithPalette(IEnumerable{Color},IList{Color})"/>
        public IEnumerable<IndexData> DecomposeWithPalette(IEnumerable<Color> colors, IList<Color> palette)
        {
            var paletteKeys = palette.Select(c => c.ToArgb() & 0x00FFFFFF).ToList();
            foreach (var c in colors)
            {
                var colorKey = c.ToArgb() & 0x00FFFFFF;

                var index = paletteKeys.IndexOf(colorKey);

                yield return new AlphaIndexData(c.A, index);
            }
        }

        /// <inheritdoc cref="IIndexEncoding.Quantize(IEnumerable{Color},QuantizationSettings)"/>
        public (IEnumerable<IndexData> indices, IList<Color> palette) Quantize(IEnumerable<Color> colors, QuantizationSettings settings)
        {
            if (settings.ColorModel == ColorModel.RGBA)
                settings.ColorModel = ColorModel.RGB;

            var colorList = colors.ToList();
            var (indices, palette) = Kolors.Quantize(colorList, settings);

            var alphaIndexData = indices.
                Zip(colorList, (x, y) => new { index = x, alpha = y.A }).
                Select(x => new AlphaIndexData(Convert.ChangeBitDepth(x.alpha, 8, _alphaDepth), x.index));
            return (alphaIndexData, palette);
        }

        /// <inheritdoc cref="IIndexEncoding.Save(IEnumerable{IndexData})"/>
        public byte[] Save(IEnumerable<IndexData> indices)
        {
            var alphaShift = _indexDepth;
            var indexBitMask = (1 << _indexDepth) - 1;

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.ASCII, true))
                foreach (var indexData in indices)
                {
                    var alphaIndexData = (AlphaIndexData)indexData;

                    long value = (alphaIndexData.Alpha << alphaShift) | (alphaIndexData.Index & indexBitMask);
                    switch (_indexDepth)
                    {
                        case 8:
                            bw.Write((byte)value);
                            break;
                        default:
                            throw new Exception($"IndexDepth {_indexDepth} not supported!");
                    }
                }

            return ms.ToArray();
        }
    }
}
