using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            if (indexDepth % 8 != 0)
                throw new InvalidOperationException("IndexDepth needs to be dividable by 8.");

            _allowAlpha = allowAlpha;
            _indexDepth = indexDepth;

            UpdateName();
        }

        private void UpdateName()
        {
            FormatName = $"I{_indexDepth}";
        }

        /// <inheritdoc cref="IIndexEncoding.Load(byte[])"/>
        public IEnumerable<IndexData> Load(byte[] input)
        {
            using (var br = new BinaryReader(new MemoryStream(input)))
                while (br.BaseStream.Position < br.BaseStream.Length)
                    switch (_indexDepth)
                    {
                        case 4:
                            var value = br.ReadByte();
                            yield return new IndexData((value >> 4) & 0xF);
                            yield return new IndexData(value & 0xF);
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
            => indices.Select(indexData => palette[indexData.Index]);

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

        /// <inheritdoc cref="IIndexEncoding.Quantize(IEnumerable{Color},QuantizationSettings)"/>
        public (IEnumerable<IndexData> indices, IList<Color> palette) Quantize(IEnumerable<Color> colors, QuantizationSettings settings)
        {
            var (indices, palette) = Kolors.Quantize(colors, settings);

            return (indices.Select(x => new IndexData(x)), palette);
        }

        /// <inheritdoc cref="IIndexEncoding.Save(IEnumerable{IndexData})"/>
        public byte[] Save(IEnumerable<IndexData> indices)
        {
            byte nibbleBuffer = 0;
            bool writeNibble = false;

            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.ASCII, true))
            {
                foreach (var indexData in indices)
                    switch (_indexDepth)
                    {
                        case 4:
                            if (writeNibble)
                            {
                                nibbleBuffer |= (byte)(indexData.Index & 0xF);
                                bw.Write(nibbleBuffer);
                            }
                            else
                                nibbleBuffer = (byte)((indexData.Index & 0xF) << 4);

                            writeNibble = !writeNibble;
                            break;
                        case 8:
                            bw.Write((byte)indexData.Index);
                            break;
                        default:
                            throw new Exception($"IndexDepth {_indexDepth} not supported!");
                    }

                if (writeNibble)
                    bw.Write(nibbleBuffer);
            }

            return ms.ToArray();
        }
    }
}
