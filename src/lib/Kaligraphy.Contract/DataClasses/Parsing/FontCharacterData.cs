namespace Kaligraphy.Contract.DataClasses.Parsing
{
    public class FontCharacterData : CharacterData
    {
        public required ushort Character { get; init; }
        public override bool IsVisible => true;
    }
}
