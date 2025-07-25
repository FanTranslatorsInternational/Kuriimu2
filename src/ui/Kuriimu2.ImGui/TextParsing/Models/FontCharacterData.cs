namespace Kuriimu2.ImGui.TextParsing.Models
{
    public class FontCharacterData : CharacterData
    {
        public required ushort Character { get; init; }
        public override bool IsVisible => true;
    }
}
