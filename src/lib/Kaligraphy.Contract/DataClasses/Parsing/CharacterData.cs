namespace Kaligraphy.Contract.DataClasses.Parsing;

public abstract class CharacterData
{
    public bool IsVisible { get; set; } = true;
    public bool IsPersistent { get; set; } = true;
}