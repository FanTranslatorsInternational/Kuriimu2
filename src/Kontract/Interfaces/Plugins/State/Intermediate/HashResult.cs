namespace Kontract.Interfaces.Plugins.State.Intermediate
{
    public class HashResult
    {
        public bool IsSuccessful { get; }
        public byte[] Result { get; }

        public HashResult(bool successful, byte[] hash)
        {
            IsSuccessful = successful;
            Result = hash;
        }
    }
}
