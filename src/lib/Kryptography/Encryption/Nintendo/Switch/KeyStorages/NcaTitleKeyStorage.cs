using System.Text.RegularExpressions;

namespace Kryptography.Encryption.Nintendo.Switch.KeyStorages
{
    /// <summary>
    /// Storage for decrypted title keys paired to their title id
    /// </summary>
    internal partial class NcaTitleKeyStorage
    {
        [GeneratedRegex("^[a-fA-F0-9]+$", RegexOptions.Compiled)]
        private static partial Regex KeyRegex();

        private readonly Dictionary<string, byte[]> _keyMaterial;

        public byte[] this[string i]
        {
            get => _keyMaterial[i];
            set => _keyMaterial[i] = value;
        }

        public NcaTitleKeyStorage(string titleKeyFile)
        {
            if (!File.Exists(titleKeyFile))
                throw new FileNotFoundException(titleKeyFile);

            _keyMaterial = LoadTitleKeys(titleKeyFile);

            ValidateKeys(_keyMaterial);
        }

        private static Dictionary<string, byte[]> LoadTitleKeys(string titleKeyFile)
        {
            return File.ReadAllLines(titleKeyFile)
                .Select(l => l.Replace(" ", "").Replace("\t", ""))
                .Where(l => !l.StartsWith(';') &&
                    !string.IsNullOrEmpty(l) &&
                    KeyRegex().IsMatch(l.Split('=').First()) &&
                    KeyRegex().IsMatch(l.Split('=').Skip(1).First()))
                .ToDictionary(
                    l => l.Split('=').First().ToUpper(),
                    l => Convert.FromHexString(l.Split('=').Skip(1).First())
                );
        }

        private static void ValidateKeys(Dictionary<string, byte[]> keyMaterial)
        {
            foreach (var key in keyMaterial)
            {
                if (key.Key.Length != 0x20) // because it's a string, it means double the length; in byte[] it would be 0x10
                    throw new InvalidOperationException($"Title id \"{key.Key}\" has an invalid length.");
                if (key.Value.Length != 0x10)
                    throw new InvalidOperationException($"Encrypted title key at title id \"{key.Key}\" has an invalid length.");
            }
        }

        public bool Contains(string key) => _keyMaterial.ContainsKey(key);
    }
}
