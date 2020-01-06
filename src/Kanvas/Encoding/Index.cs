using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the default index-only encoding.
    /// </summary>
    public class Index : IColorIndexEncoding
    {
        private readonly int _indexDepth;

        /// <inheritdoc />
        public int BitDepth { get; }

        /// <inheritdoc />
        public bool IsBlockCompression => false;

        /// <inheritdoc />
        public string FormatName { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="BitOrder"/> for this instance.
        /// </summary>
        public BitOrder BitOrder { get; set; } = BitOrder.LeastSignificantFirst;

        /// <summary>
        /// Creates a new instance of <see cref="Index"/>.
        /// </summary>
        /// <param name="indexDepth">Depth of the index component.</param>
        public Index(int indexDepth)
        {
            if (!IsPowerOfTwo(indexDepth))
                throw new InvalidOperationException("Index depth needs to be a power of 2.");

            _indexDepth = indexDepth;

            BitDepth = indexDepth;

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

        /// <inheritdoc />
        public IEnumerable<(int, Color)> Load(byte[] input)
        {
            var mask = (1 << _indexDepth) - 1;

            using (var br = new BinaryReader(new MemoryStream(input)))
                while (br.BaseStream.Position < br.BaseStream.Length)
                    switch (_indexDepth)
                    {
                        case 1:
                        case 2:
                        case 4:
                            var value4 = br.ReadByte();
                            var max = 8 / _indexDepth - 1;
                            if (BitOrder == BitOrder.MostSignificantFirst)
                                for (int i = max; i >= 0; i--)
                                    yield return ((value4 >> i * _indexDepth) & mask, Color.Empty);
                            else
                                for (int i = 0; i <= max; i++)
                                    yield return ((value4 >> i * _indexDepth) & mask, Color.Empty);
                            break;
                        case 8:
                            yield return (br.ReadByte(), Color.Empty);
                            break;
                        default:
                            throw new InvalidOperationException($"IndexDepth {_indexDepth} not supported.");
                    }
        }

        /// <inheritdoc />
        public Color GetColorFromIndex((int, Color) indexColor, IList<Color> palette)
        {
            return palette[indexColor.Item1];
        }

        /// <inheritdoc />
        public byte[] Save(IEnumerable<(int,Color)> indexColors)
        {
            byte valueBuffer = 0;
            var counter = 0;
            var mask = (1 << _indexDepth) - 1;

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.ASCII, true))
            {
                foreach (var indexData in indexColors)
                    switch (_indexDepth)
                    {
                        case 1:
                        case 2:
                        case 4:
                            counter += _indexDepth;
                            if (BitOrder == BitOrder.MostSignificantFirst)
                            {
                                valueBuffer <<= _indexDepth;
                                valueBuffer |= (byte)(indexData.Item1 & mask);
                            }
                            else
                            {
                                valueBuffer |= (byte)((indexData.Item1 & mask) << (counter - _indexDepth));
                            }

                            if (counter == 8)
                            {
                                bw.Write(valueBuffer);
                                counter = 0;
                                valueBuffer = 0;
                            }
                            break;
                        case 8:
                            bw.Write((byte)indexData.Item1);
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
