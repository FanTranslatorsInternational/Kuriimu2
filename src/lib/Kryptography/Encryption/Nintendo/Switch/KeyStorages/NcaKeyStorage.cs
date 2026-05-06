using System.Text.RegularExpressions;
using Kryptography.Encryption.AES;

namespace Kryptography.Encryption.Nintendo.Switch.KeyStorages
{
    internal partial class NcaKeyStorage
    {
        #region Regex

        [GeneratedRegex("^[a-fA-F0-9]+$", RegexOptions.Compiled)]
        private static partial Regex KeyRegex();

        [GeneratedRegex("master_key_([\\d]{2})", RegexOptions.Compiled)]
        private static partial Regex MasterKeyRegex();

        [GeneratedRegex("key_area_key_application_([\\d]{2})", RegexOptions.Compiled)]
        private static partial Regex KeyAreaKeyApplicationRegex();
        [GeneratedRegex("key_area_key_ocean_([\\d]{2})", RegexOptions.Compiled)]
        private static partial Regex KeyAreaKeyOceanRegex();
        [GeneratedRegex("key_area_key_system_([\\d]{2})", RegexOptions.Compiled)]
        private static partial Regex KeyAreaKeySystemRegex();
        [GeneratedRegex("titlekek_([\\d]{2})", RegexOptions.Compiled)]
        private static partial Regex TitleKekRegex();

        #endregion

        #region String Constants

        private const string HeaderKeyName_ = "header_key";

        private const string AesKekGenerationSourceName_ = "aes_kek_generation_source";
        private const string AesKeyGenerationSourceName_ = "aes_key_generation_source";

        private const string KeyAreaKeyApplicationName_ = "key_area_key_application_{0:X2}";
        private const string KeyAreaKeyOceanName_ = "key_area_key_ocean_{0:X2}";
        private const string KeyAreaKeySystemName_ = "key_area_key_system_{0:X2}";
        private const string KeyAreaKeyApplicationSourceName_ = "key_area_key_application_source";
        private const string KeyAreaKeyOceanSourceName_ = "key_area_key_ocean_source";
        private const string KeyAreaKeySystemSourceName_ = "key_area_key_system_source";

        private const string TitleKekName_ = "titlekek_{0:X2}";
        private const string TitleKekSourceName_ = "titlekek_source";

        #endregion

        #region Key Storages

        public Dictionary<int, byte[]> MasterKeys { get; private set; }
        public Dictionary<int, byte[]> KekApplication { get; private set; }
        public Dictionary<int, byte[]> KekOcean { get; private set; }
        public Dictionary<int, byte[]> KekSystem { get; private set; }
        public Dictionary<int, byte[]> TitleKek { get; private set; }
        public byte[] HeaderKey => _keyMaterial[HeaderKeyName_];

        #endregion Key Storages

        public bool ContainsHeaderKey => _keyMaterial.ContainsKey(HeaderKeyName_);

        private readonly Dictionary<string, byte[]> _keyMaterial;

        public byte[] this[string i]
        {
            get => _keyMaterial[i];
            set => _keyMaterial[i] = value;
        }

        public NcaKeyStorage(string keyFile)
        {
            if (!File.Exists(keyFile))
                throw new FileNotFoundException(keyFile);

            _keyMaterial = ReadKeys(keyFile);

            KekApplication = LoadKekApplications(_keyMaterial);
            KekOcean = LoadKekOceans(_keyMaterial);
            KekSystem = LoadKekSystems(_keyMaterial);
            TitleKek = LoadTitleKeks(_keyMaterial);

            MasterKeys = LoadMasterKeys(_keyMaterial);
            GenerateKeyMaterial(MasterKeys, _keyMaterial);
        }

        private static Dictionary<string, byte[]> ReadKeys(string keyFile)
        {
            return File.ReadAllLines(keyFile)
                .Select(l => l.Replace(" ", "").Replace("\t", ""))
                .Where(l => !l.StartsWith(';') && !string.IsNullOrEmpty(l) && KeyRegex().IsMatch(l.Split('=').Skip(1).First()))
                .ToDictionary(
                    l => l.Split('=').First(),
                    l => Convert.FromHexString(l.Split('=').Skip(1).First())
                    );
        }

        private static Dictionary<int, byte[]> LoadMasterKeys(Dictionary<string, byte[]> keyMaterial)
        {
            return new Dictionary<int, byte[]>(keyMaterial
                .Where(x => MasterKeyRegex().IsMatch(x.Key))
                .ToDictionary(x => Convert.ToInt32(MasterKeyRegex().Match(x.Key).Groups[1].Value), y => y.Value));
        }

        private static void GenerateKeyMaterial(Dictionary<int, byte[]> masterKeys, Dictionary<string, byte[]> keyMaterial)
        {
            var aesKekGenSource = keyMaterial[AesKekGenerationSourceName_];
            var aesKeyGenSource = keyMaterial[AesKeyGenerationSourceName_];

            foreach (var masterKey in masterKeys)
            {
                var keyAreaKeyApplicationName = string.Format(KeyAreaKeyApplicationName_, masterKey.Key);
                var keyAreaKeyOceanName = string.Format(KeyAreaKeyOceanName_, masterKey.Key);
                var keyAreaKeySystemName = string.Format(KeyAreaKeySystemName_, masterKey.Key);
                var titleKekName = string.Format(TitleKekName_, masterKey.Key);

                if (!keyMaterial.ContainsKey(keyAreaKeyApplicationName))
                    keyMaterial[keyAreaKeyApplicationName] = GenerateKek(keyMaterial[KeyAreaKeyApplicationSourceName_], masterKey.Value, aesKekGenSource, aesKeyGenSource);
                if (!keyMaterial.ContainsKey(keyAreaKeyOceanName))
                    keyMaterial[keyAreaKeyOceanName] = GenerateKek(keyMaterial[KeyAreaKeyOceanSourceName_], masterKey.Value, aesKekGenSource, aesKeyGenSource);
                if (!keyMaterial.ContainsKey(keyAreaKeySystemName))
                    keyMaterial[keyAreaKeySystemName] = GenerateKek(keyMaterial[KeyAreaKeySystemSourceName_], masterKey.Value, aesKekGenSource, aesKeyGenSource);

                if (!keyMaterial.ContainsKey(titleKekName))
                {
                    keyMaterial[titleKekName] = new byte[keyMaterial[TitleKekSourceName_].Length];
                    var ecb = new EcbStream(new MemoryStream(keyMaterial[TitleKekSourceName_]), masterKey.Value);
                    _ = ecb.Read(keyMaterial[titleKekName], 0, keyMaterial[titleKekName].Length);
                }
            }
        }

        private static Dictionary<int, byte[]> LoadKekApplications(Dictionary<string, byte[]> keyMaterial)
        {
            return new Dictionary<int, byte[]>(keyMaterial
                .Where(x => KeyAreaKeyApplicationRegex().IsMatch(x.Key))
                .ToDictionary(x => Convert.ToInt32(KeyAreaKeyApplicationRegex().Match(x.Key).Groups[1].Value), y => y.Value));
        }

        private static Dictionary<int, byte[]> LoadKekOceans(Dictionary<string, byte[]> keyMaterial)
        {
            return new Dictionary<int, byte[]>(keyMaterial
                .Where(x => KeyAreaKeyOceanRegex().IsMatch(x.Key))
                .ToDictionary(x => Convert.ToInt32(KeyAreaKeyOceanRegex().Match(x.Key).Groups[1].Value), y => y.Value));
        }

        private static Dictionary<int, byte[]> LoadKekSystems(Dictionary<string, byte[]> keyMaterial)
        {
            return new Dictionary<int, byte[]>(keyMaterial
                .Where(x => KeyAreaKeySystemRegex().IsMatch(x.Key))
                .ToDictionary(x => Convert.ToInt32(KeyAreaKeySystemRegex().Match(x.Key).Groups[1].Value), y => y.Value));
        }

        private static Dictionary<int, byte[]> LoadTitleKeks(Dictionary<string, byte[]> keyMaterial)
        {
            return new Dictionary<int, byte[]>(keyMaterial
                .Where(x => TitleKekRegex().IsMatch(x.Key))
                .ToDictionary(x => Convert.ToInt32(TitleKekRegex().Match(x.Key).Groups[1].Value), y => y.Value));
        }

        private static byte[] GenerateKek(byte[] generationSource, byte[] masterKey, byte[] aesKekGenSource, byte[]? aesKeyGenSource)
        {
            var kek = new byte[16];

            _ = new EcbStream(new MemoryStream(aesKekGenSource), masterKey).Read(kek, 0, kek.Length);
            _ = new EcbStream(new MemoryStream(generationSource), kek).Read(kek, 0, kek.Length);

            if (aesKeyGenSource != null)
                _ = new EcbStream(new MemoryStream(aesKeyGenSource), kek).Read(kek, 0, kek.Length);

            return kek;
        }
    }
}
