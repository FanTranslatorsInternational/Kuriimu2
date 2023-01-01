namespace Kontract.Models.IO
{
    /// <summary>
    /// The bit order in which values should be read.
    /// </summary>
    public enum BitOrder
    {
        Default,
        MostSignificantBitFirst,
        LeastSignificantBitFirst,
        LowestAddressFirst,
        HighestAddressFirst
    }
}
