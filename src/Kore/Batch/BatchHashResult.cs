namespace Kore.Batch
{
    public class BatchHashResult
    {
        public bool IsSuccessful { get; }
        public string File { get; }
        public byte[] Result { get; }

        public BatchHashResult(bool successful, string file, byte[] hash)
        {
            IsSuccessful = successful;
            File = file;
            Result = hash;
        }
    }
}
