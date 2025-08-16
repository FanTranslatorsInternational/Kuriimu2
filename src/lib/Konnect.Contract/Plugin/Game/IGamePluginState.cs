using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Konnect.Contract.Plugin.Game
{
    public interface IGamePluginState
    {
        ICharacterParser? Parser { get; }
        ICharacterComposer? Composer { get; }
        ICharacterSerializer? Serializer { get; }
        ICharacterDeserializer? Deserializer { get; }

        Task<IList<Image<Rgba32>>?> CreatePreviewPages(IList<IList<CharacterData>> characters);
    }
}
