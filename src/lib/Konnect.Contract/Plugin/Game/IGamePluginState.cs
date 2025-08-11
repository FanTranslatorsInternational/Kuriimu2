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

        Task<Image<Rgba32>?> CreatePreview(IList<CharacterData> characters);
    }
}
