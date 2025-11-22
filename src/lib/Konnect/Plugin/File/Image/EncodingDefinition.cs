using Kanvas.Contract;
using Kanvas.Contract.Encoding;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Image;

namespace Konnect.Plugin.File.Image;

public class EncodingDefinition : IEncodingDefinition
{
    private readonly Dictionary<int, IColorEncoding> _colorEncodings;
    private readonly Dictionary<int, IndexEncodingDefinition> _indexEncodings;
    private readonly Dictionary<int, IColorEncoding> _paletteEncodings;

    private readonly Dictionary<int, IColorShader> _colorShaders;
    private readonly Dictionary<int, IColorShader> _paletteShaders;

    public static EncodingDefinition Empty { get; } = new();

    public bool IsEmpty => _colorEncodings.Keys.Count <= 0 && _indexEncodings.Keys.Count <= 0 && _paletteEncodings.Keys.Count <= 0;

    public IReadOnlyDictionary<int, IColorEncoding> ColorEncodings => _colorEncodings;

    public IReadOnlyDictionary<int, IndexEncodingDefinition> IndexEncodings => _indexEncodings;

    public IReadOnlyDictionary<int, IColorEncoding> PaletteEncodings => _paletteEncodings;

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

    public IColorEncoding? GetColorEncoding(int imageFormat)
    {
        return ContainsColorEncoding(imageFormat) ? _colorEncodings[imageFormat] : null;
    }

    public bool ContainsPaletteEncoding(int paletteFormat)
    {
        return _paletteEncodings.ContainsKey(paletteFormat);
    }

    public IColorEncoding? GetPaletteEncoding(int paletteFormat)
    {
        return ContainsPaletteEncoding(paletteFormat) ? _paletteEncodings[paletteFormat] : null;
    }

    public bool ContainsIndexEncoding(int indexFormat)
    {
        return _indexEncodings.ContainsKey(indexFormat);
    }

    public IndexEncodingDefinition? GetIndexEncoding(int indexFormat)
    {
        return ContainsIndexEncoding(indexFormat) ? _indexEncodings[indexFormat] : null;
    }

    public bool ContainsColorShader(int imageFormat)
    {
        return _colorShaders.ContainsKey(imageFormat);
    }

    public IColorShader? GetColorShader(int imageFormat)
    {
        return ContainsColorShader(imageFormat) ? _colorShaders[imageFormat] : null;
    }

    public bool ContainsPaletteShader(int paletteFormat)
    {
        return _paletteShaders.ContainsKey(paletteFormat);
    }

    public IColorShader? GetPaletteShader(int paletteFormat)
    {
        return ContainsPaletteShader(paletteFormat) ? _paletteShaders[paletteFormat] : null;
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
        AddIndexEncodingInternal(indexFormat, new IndexEncodingDefinition
        {
            IndexEncoding = indexEncoding,
            PaletteEncodingIndices = paletteFormatIndices
        });
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
}