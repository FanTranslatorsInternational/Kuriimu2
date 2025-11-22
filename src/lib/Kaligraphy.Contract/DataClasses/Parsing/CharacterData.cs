namespace Kaligraphy.Contract.DataClasses.Parsing;

public abstract class CharacterData
{
    public bool IsVisible { get; init; } = true;
    public bool IsPersistent { get; init; } = true;
}