using System;
using System.Collections.Generic;
using System.Linq;
using Kontract.Kanvas;

namespace Kontract.Models.Image
{
    public class IndexEncodingDefinition
    {
        public IIndexEncoding IndexEncoding { get; }
        public IList<int> PaletteEncodingIndices { get; }

        public IndexEncodingDefinition(IIndexEncoding indexEncoding, IList<int> paletteEncodingIndices)
        {
            ContractAssertions.IsNotNull(indexEncoding, nameof(indexEncoding));
            ContractAssertions.IsNotNull(paletteEncodingIndices, nameof(paletteEncodingIndices));

            IndexEncoding = indexEncoding;
            PaletteEncodingIndices = paletteEncodingIndices;
        }
    }

    public static class EncodingDefinitionExtensions
    {
        public static EncodingDefinition ToColorDefinition(this IDictionary<int, IColorEncoding> mappings)
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddColorEncodings(mappings);

            return encodingDefinition;
        }

        public static EncodingDefinition ToIndexDefinition(this IDictionary<int, IndexEncodingDefinition> mappings)
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddIndexEncodings(mappings);

            return encodingDefinition;
        }

        public static EncodingDefinition ToPaletteDefinition(this IDictionary<int, IColorEncoding> mappings)
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddPaletteEncodings(mappings);

            return encodingDefinition;
        }
    }

    public class EncodingDefinition
    {
        private readonly Dictionary<int, IColorEncoding> _colorEncodings;
        private readonly Dictionary<int, IndexEncodingDefinition> _indexEncodings;
        private readonly Dictionary<int, IColorEncoding> _paletteEncodings;

        public static EncodingDefinition Empty { get; } = new EncodingDefinition();

        public bool IsEmpty => !HasColorEncodings && !HasIndexEncodings && !HasPaletteEncodings;

        public IReadOnlyDictionary<int, IColorEncoding> ColorEncodings => _colorEncodings;

        public IReadOnlyDictionary<int, IndexEncodingDefinition> IndexEncodings => _indexEncodings;

        public IReadOnlyDictionary<int, IColorEncoding> PaletteEncodings => _paletteEncodings;

        public bool HasColorEncodings => _colorEncodings.Any();

        public bool HasIndexEncodings => _indexEncodings.Any();

        public bool HasPaletteEncodings => _paletteEncodings.Any();

        public EncodingDefinition()
        {
            _colorEncodings = new Dictionary<int, IColorEncoding>();
            _paletteEncodings = new Dictionary<int, IColorEncoding>();
            _indexEncodings = new Dictionary<int, IndexEncodingDefinition>();
        }

        public bool ContainsColorEncoding(int imageFormat)
        {
            return _colorEncodings.ContainsKey(imageFormat);
        }

        public IColorEncoding GetColorEncoding(int imageFormat)
        {
            return ContainsColorEncoding(imageFormat) ? _colorEncodings[imageFormat] : null;
        }

        public bool ContainsPaletteEncoding(int paletteFormat)
        {
            return _paletteEncodings.ContainsKey(paletteFormat);
        }

        public IColorEncoding GetPaletteEncoding(int paletteFormat)
        {
            return ContainsPaletteEncoding(paletteFormat) ? _paletteEncodings[paletteFormat] : null;
        }

        public bool ContainsIndexEncoding(int indexFormat)
        {
            return _indexEncodings.ContainsKey(indexFormat);
        }

        public IndexEncodingDefinition GetIndexEncoding(int indexFormat)
        {
            return ContainsIndexEncoding(indexFormat) ? _indexEncodings[indexFormat] : null;
        }

        public bool Supports(ImageInfo imageInfo)
        {
            var isColorEncoding = _colorEncodings.ContainsKey(imageInfo.ImageFormat);
            var isIndexColorEncoding = _indexEncodings.ContainsKey(imageInfo.ImageFormat);
            if (!isColorEncoding && !isIndexColorEncoding)
                return false;
            //throw new InvalidOperationException($"Image format {imageInfo.ImageFormat} is not supported by the plugin.");

            if (isIndexColorEncoding && !imageInfo.HasPaletteInformation)
                return false;
            //throw new InvalidOperationException($"The image format {image.ImageFormat} is indexed, but the image is not.");

            if (isColorEncoding && imageInfo.HasPaletteInformation)
                return false;
            //throw new InvalidOperationException($"The image format {image.ImageFormat} is not indexed, but the image is.");

            return true;
        }

        public void AddColorEncoding(int imageFormat, IColorEncoding colorEncoding)
        {
            if (_colorEncodings.ContainsKey(imageFormat) || _indexEncodings.ContainsKey(imageFormat))
                return;

            _colorEncodings.Add(imageFormat, colorEncoding);
        }

        public void AddColorEncodings(IList<(int, IColorEncoding)> colorEncodings)
        {
            foreach (var colorEncoding in colorEncodings)
                AddColorEncoding(colorEncoding.Item1, colorEncoding.Item2);
        }

        public void AddColorEncodings(IDictionary<int, IColorEncoding> colorEncodings)
        {
            foreach (var colorEncoding in colorEncodings)
                AddColorEncoding(colorEncoding.Key, colorEncoding.Value);
        }

        public void AddPaletteEncoding(int paletteFormat, IColorEncoding paletteEncoding)
        {
            if (_paletteEncodings.ContainsKey(paletteFormat))
                return;

            _paletteEncodings.Add(paletteFormat, paletteEncoding);
        }

        public void AddPaletteEncodings(IList<(int, IColorEncoding)> paletteEncodings)
        {
            foreach (var paletteEncoding in paletteEncodings)
                AddPaletteEncoding(paletteEncoding.Item1, paletteEncoding.Item2);
        }

        public void AddPaletteEncodings(IDictionary<int, IColorEncoding> paletteEncodings)
        {
            foreach (var paletteEncoding in paletteEncodings)
                AddPaletteEncoding(paletteEncoding.Key, paletteEncoding.Value);
        }

        public void AddIndexEncoding(int indexFormat, IIndexEncoding indexEncoding, IList<int> paletteFormatIndices)
        {
            AddIndexEncodingInternal(indexFormat, new IndexEncodingDefinition(indexEncoding, paletteFormatIndices));
        }

        public void AddIndexEncodings(IList<(int, IndexEncodingDefinition)> indexEncodings)
        {
            foreach (var indexEncoding in indexEncodings)
                AddIndexEncodingInternal(indexEncoding.Item1, indexEncoding.Item2);
        }

        public void AddIndexEncodings(IDictionary<int, IndexEncodingDefinition> indexEncodings)
        {
            foreach (var indexEncoding in indexEncodings)
                AddIndexEncodingInternal(indexEncoding.Key, indexEncoding.Value);
        }

        private void AddIndexEncodingInternal(int indexFormat, IndexEncodingDefinition indexDefinition)
        {
            if (_indexEncodings.ContainsKey(indexFormat) || _colorEncodings.ContainsKey(indexFormat) || !indexDefinition.PaletteEncodingIndices.Any())
                return;

            if (indexDefinition.PaletteEncodingIndices.Any(x => !_paletteEncodings.ContainsKey(x)))
                throw new InvalidOperationException("Some palette encodings are not supported.");

            _indexEncodings.Add(indexFormat, indexDefinition);
        }
    }
}
