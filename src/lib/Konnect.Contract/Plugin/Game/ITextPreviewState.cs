using Kaligraphy.Contract.DataClasses.Parsing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Konnect.Contract.Plugin.Game;

public interface ITextPreviewState : ITextProcessingState
{
    Task<IList<Image<Rgba32>>?> RenderPreviews(IList<IList<CharacterData>> characters);
}