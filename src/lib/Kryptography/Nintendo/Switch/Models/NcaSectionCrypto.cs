namespace Kryptography.Nintendo.Switch.Models
{
    public enum NcaSectionCrypto:byte
    {
        NoCrypto = 1,
        Xts,
        Ctr,
        Bktr,
        TitleKey = 255
    }
}
