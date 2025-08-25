using Kaligraphy.Contract.Parsing;
using Konnect.Contract.Plugin.Game;
using plugin_mt_framework_preview.Texts;

namespace plugin_mt_framework_preview.Previews
{
    class GenericGmdState : ITextProcessingState
    {
        public ICharacterParser? Parser { get; } = new GmdCharacterParser();
        public ICharacterComposer? Composer { get; } = new GmdCharacterComposer();
        public ICharacterSerializer? Serializer { get; } = new GmdCharacterSerializer();
        public ICharacterDeserializer? Deserializer { get; } = new GmdCharacterDeserializer();
    }
}
