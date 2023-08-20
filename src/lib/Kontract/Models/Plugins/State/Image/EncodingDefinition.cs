using System;
using System.Collections.Generic;
using System.Linq;
using Kontract.Kanvas.Interfaces;

namespace Kontract.Models.Plugins.State.Image
{
    public static class EncodingDefinitionExtensions
    {
        public static EncodingDefinition ToColorDefinition(this IDictionary<int, IColorEncoding> mappings)
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddColorEncodings(mappings);

            return encodingDefinition;
        }

        public static EncodingDefinition ToIndexDefinition(this IDictionary<int, EncodingDefinition.IndexEncodingDefinition> mappings)
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

        private readonly Dictionary<int, IColorShader> _colorShaders;
        private readonly Dictionary<int, IColorShader> _paletteShaders;

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

            _colorShaders = new Dictionary<int, IColorShader>();
            _paletteShaders = new Dictionary<int, IColorShader>();
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

        public bool ContainsColorShader(int imageFormat)
        {
            return _colorShaders.ContainsKey(imageFormat);
        }

        public IColorShader GetColorShader(int imageFormat)
        {
            return _colorShaders[imageFormat];
        }

        public bool ContainsPaletteShader(int paletteFormat)
        {
            return _paletteShaders.ContainsKey(paletteFormat);
        }

        public IColorShader GetPaletteShader(int paletteFormat)
        {
            return _paletteShaders[paletteFormat];
        }

        public bool Supports(ImageData imageData, out string error)
        {
            error = string.Empty;

            var isColorEncoding = _colorEncodings.ContainsKey(imageData.Format);
            var isIndexColorEncoding = _indexEncodings.ContainsKey(imageData.Format);
            if (!isColorEncoding && !isIndexColorEncoding)
            {
                error = $"Format {imageData.Format} is not supported by the encoding definition.";
                return false;
            }

            if (isIndexColorEncoding && !imageData.HasPaletteInformation)
            {
                error = "The image requires palette data, but it doesn't contain any.";
                return false;
            }

            return true;
        }

        #region Add color formats

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

        #endregion

        #region Add palette formats

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

        #endregion

        #region Add index formats

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

            var missingPaletteEncodings = indexDefinition.PaletteEncodingIndices.Where(x => !_paletteEncodings.ContainsKey(x)).ToArray();
            if (missingPaletteEncodings.Length > 0)
                throw new InvalidOperationException($"Palette encodings {string.Join(", ", missingPaletteEncodings)} are not supported by the encoding definition.");

            _indexEncodings.Add(indexFormat, indexDefinition);
        }

        #endregion

        #region Add color shaders

        public void AddColorShader(int imageFormat, IColorShader colorShader)
        {
            if (_colorShaders.ContainsKey(imageFormat))
                return;

            _colorShaders.Add(imageFormat, colorShader);
        }

        public void AddColorShaders(IList<(int, IColorShader)> colorShaders)
        {
            foreach (var colorShader in colorShaders)
                AddColorShader(colorShader.Item1, colorShader.Item2);
        }

        public void AddColorShaders(IDictionary<int, IColorShader> colorShaders)
        {
            foreach (var colorShader in colorShaders)
                AddColorShader(colorShader.Key, colorShader.Value);
        }

        public void AddPaletteShader(int imageFormat, IColorShader paletteShader)
        {
            if (_paletteShaders.ContainsKey(imageFormat))
                return;

            _paletteShaders.Add(imageFormat, paletteShader);
        }

        public void AddPaletteShaders(IList<(int, IColorShader)> paletteShaders)
        {
            foreach (var paletteShader in paletteShaders)
                AddPaletteShader(paletteShader.Item1, paletteShader.Item2);
        }

        public void AddPaletteShaders(IDictionary<int, IColorShader> paletteShaders)
        {
            foreach (var paletteShader in paletteShaders)
                AddPaletteShader(paletteShader.Key, paletteShader.Value);
        }

        #endregion

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
    }
}
