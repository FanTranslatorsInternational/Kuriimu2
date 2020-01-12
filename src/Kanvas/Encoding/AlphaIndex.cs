using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kontract.Kanvas;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the default index-only encoding.
    /// </summary>
    public class AlphaIndex : IColorIndexEncoding
    {
        private readonly int _alphaDepth;
        private readonly int _indexDepth;
        private readonly int _bitDepth;

        /// <inheritdoc />
        public int BitDepth => _bitDepth;

        /// <inheritdoc />
        public bool IsBlockCompression => false;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IEnumerable<(int, Color)> Load(byte[] input)
        {
            var alphaShift = _indexDepth;
            var indexBitMask = (1 << _indexDepth) - 1;

            using (var br = new BinaryReader(new MemoryStream(input)))
                while (br.BaseStream.Position < br.BaseStream.Length)
                    switch (_bitDepth)
                    {
                        case 8:
                            var value = br.ReadByte();
                            yield return (value & indexBitMask,
                                Color.FromArgb(Kanvas.Support.Conversion.ChangeBitDepth(value >> alphaShift, _alphaDepth, 8), 0, 0, 0));
                            break;
                        default:
                            throw new InvalidOperationException($"IndexDepth {_indexDepth} not supported.");
                    }
        }

        public Color GetColorFromIndex((int, Color) indexColor, IList<Color> palette)
        {
            var paletteColor = palette[indexColor.Item1];
            return Color.FromArgb(indexColor.Item2.A, paletteColor.R, paletteColor.G, paletteColor.B);
        }

        /// <inheritdoc />
        public byte[] Save(IEnumerable<(int, Color)> indexColors)
        {
            var alphaShift = _indexDepth;
            var indexBitMask = (1 << _indexDepth) - 1;

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.ASCII, true))
                foreach (var indexData in indexColors)
                {
                    long value = (indexData.Item2.A << alphaShift) | (indexData.Item1 & indexBitMask);
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
