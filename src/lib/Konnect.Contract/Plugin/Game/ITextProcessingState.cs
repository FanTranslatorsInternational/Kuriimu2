using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Konnect.Contract.Plugin.Game;

public interface ITextProcessingState : IGamePluginState
{
    ICharacterParser? Parser { get; }
    ICharacterComposer? Composer { get; }
    ICharacterSerializer? Serializer { get; }
    ICharacterDeserializer? Deserializer { get; }

    #region Optional feature checks

    public bool CanRenderPreviews => this is ITextPreviewState;

    #endregion

    #region Optional feature casting defaults

    Task<IList<Image<Rgba32>>?> AttemptRenderPreviews(IList<IList<CharacterData>> characters) =>
        (this as ITextPreviewState)?.RenderPreviews(characters) ?? Task.FromResult<IList<Image<Rgba32>>?>(null);

    #endregion
}