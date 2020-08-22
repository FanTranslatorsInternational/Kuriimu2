using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kryptography.AES;
using Kryptography.Extensions;

namespace Kryptography.Nintendo.Switch.KeyStorages
{
    class NcaKeyStorage
    {
        #region Regex

        private static readonly Regex MasterKeyRegex = new Regex("master_key_([\\d]{2})");

        private static readonly Regex KeyAreaKeyApplicationRegex = new Regex("key_area_key_application_([\\d]{2})");
        private static readonly Regex KeyAreaKeyOceanRegex = new Regex("key_area_key_ocean_([\\d]{2})");
        private static readonly Regex KeyAreaKeySystemRegex = new Regex("key_area_key_system_([\\d]{2})");
        private static readonly Regex TitleKekRegex = new Regex("titlekek_([\\d]{2})");

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

        private Dictionary<string, byte[]> _keyMaterial;

        public byte[] this[string i]
        {
            get
            {
                if (!_keyMaterial.ContainsKey(i))
                    throw new KeyNotFoundException(i);
                return _keyMaterial[i];
            }
            set
            {
                if (!_keyMaterial.ContainsKey(i))
                    _keyMaterial.Add(i, value);
                else
                    _keyMaterial[i] = value;
            }
        }

        public NcaKeyStorage(string keyFile)
        {
            if (!File.Exists(keyFile))
                throw new FileNotFoundException(keyFile);

            LoadKeys(keyFile);

            LoadMasterKeys();
            LoadKeks();
        }

        private void LoadKeys(string keyFile)
        {
            _keyMaterial = File.ReadAllLines(keyFile)
                .Select(l => l.Replace(" ", "").Replace("\t", ""))
                .Where(l => !l.StartsWith(";") && !string.IsNullOrEmpty(l) && Regex.IsMatch(l.Split('=').Skip(1).First(), "^[a-fA-F0-9]+$"))
                .ToDictionary(
                    l => l.Split('=').First(),
                    l => l.Split('=').Skip(1).First().Hexlify()
                    );
        }

        private void LoadMasterKeys()
        {
            MasterKeys = new Dictionary<int, byte[]>(_keyMaterial
                .Where(x => MasterKeyRegex.IsMatch(x.Key))
                .ToDictionary(x => Convert.ToInt32(MasterKeyRegex.Match(x.Key).Groups[1].Value), y => y.Value));
        }

        private void LoadKeks()
        {
            var aesKekGenSource = this[AesKekGenerationSourceName_];
            var aesKeyGenSource = this[AesKeyGenerationSourceName_];

            foreach (var masterKey in MasterKeys)
            {
                var keyAreaKeyApplicationName = string.Format(KeyAreaKeyApplicationName_, masterKey.Key);
                var keyAreaKeyOceanName = string.Format(KeyAreaKeyOceanName_, masterKey.Key);
                var keyAreaKeySystemName = string.Format(KeyAreaKeySystemName_, masterKey.Key);
                var titleKekName = string.Format(TitleKekName_, masterKey.Key);

                if (!_keyMaterial.ContainsKey(keyAreaKeyApplicationName))
                    this[keyAreaKeyApplicationName] = GenerateKek(this[KeyAreaKeyApplicationSourceName_], masterKey.Value, aesKekGenSource, aesKeyGenSource);
                if (!_keyMaterial.ContainsKey(keyAreaKeyOceanName))
                    this[keyAreaKeyOceanName] = GenerateKek(this[KeyAreaKeyOceanSourceName_], masterKey.Value, aesKekGenSource, aesKeyGenSource);
                if (!_keyMaterial.ContainsKey(keyAreaKeySystemName))
                    this[keyAreaKeySystemName] = GenerateKek(this[KeyAreaKeySystemSourceName_], masterKey.Value, aesKekGenSource, aesKeyGenSource);

                if (!_keyMaterial.ContainsKey(titleKekName))
                {
                    this[titleKekName] = new byte[this[TitleKekSourceName_].Length];
                    var ecb = new EcbStream(new MemoryStream(this[TitleKekSourceName_]), masterKey.Value);
                    ecb.Read(this[titleKekName], 0, this[titleKekName].Length);
                }
            }

            KekApplication = new Dictionary<int, byte[]>(_keyMaterial
                .Where(x => KeyAreaKeyApplicationRegex.IsMatch(x.Key))
                .ToDictionary(x => Convert.ToInt32(KeyAreaKeyApplicationRegex.Match(x.Key).Groups[1].Value), y => y.Value));
            KekOcean = new Dictionary<int, byte[]>(_keyMaterial
                .Where(x => KeyAreaKeyOceanRegex.IsMatch(x.Key))
                .ToDictionary(x => Convert.ToInt32(KeyAreaKeyOceanRegex.Match(x.Key).Groups[1].Value), y => y.Value));
            KekSystem = new Dictionary<int, byte[]>(_keyMaterial
                .Where(x => KeyAreaKeySystemRegex.IsMatch(x.Key))
                .ToDictionary(x => Convert.ToInt32(KeyAreaKeySystemRegex.Match(x.Key).Groups[1].Value), y => y.Value));
            TitleKek = new Dictionary<int, byte[]>(_keyMaterial
                .Where(x => TitleKekRegex.IsMatch(x.Key))
                .ToDictionary(x => Convert.ToInt32(TitleKekRegex.Match(x.Key).Groups[1].Value), y => y.Value));
        }

        private byte[] GenerateKek(byte[] generationSource, byte[] masterKey, byte[] aesKekGenSource, byte[] aesKeyGenSource)
        {
            var kek = new byte[16];

            new EcbStream(new MemoryStream(aesKekGenSource), masterKey).Read(kek, 0, kek.Length);
            new EcbStream(new MemoryStream(generationSource), kek).Read(kek, 0, kek.Length);

            if (aesKeyGenSource != null)
                new EcbStream(new MemoryStream(aesKeyGenSource), kek).Read(kek, 0, kek.Length);

            return kek;
        }
    }
}
