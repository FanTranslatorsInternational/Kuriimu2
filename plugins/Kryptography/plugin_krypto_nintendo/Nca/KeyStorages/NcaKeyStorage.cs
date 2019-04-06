using Komponent.IO;
using Kryptography.AES;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca.KeyStorages
{
    internal class NcaKeyStorage
    {
        #region Key Storages

        public Dictionary<int, byte[]> MasterKeys { get; private set; }
        public Dictionary<int, byte[]> KekApplication { get; private set; }
        public Dictionary<int, byte[]> KekOcean { get; private set; }
        public Dictionary<int, byte[]> KekSystem { get; private set; }
        public Dictionary<int, byte[]> TitleKek { get; private set; }
        public byte[] HeaderKey => _keyMaterial["header_key"];

        #endregion Key Storages

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
                .Where(x => Regex.IsMatch(x.Key, "master_key_[\\d]{2}"))
                .ToDictionary(x => Convert.ToInt32(x.Key.Replace("master_key_", "")), y => y.Value));
        }

        private void LoadKeks()
        {
            foreach (var masterKey in MasterKeys)
            {
                if (!_keyMaterial.ContainsKey($"key_area_key_application_{masterKey.Key:X2}"))
                    this[$"key_area_key_application_{masterKey.Key:X2}"] = GenerateKek(this["key_area_key_application_source"], masterKey.Value, this["aes_kek_generation_source"], this["aes_key_generation_source"]);
                if (!_keyMaterial.ContainsKey($"key_area_key_ocean_{masterKey.Key:X2}"))
                    this[$"key_area_key_ocean_{masterKey.Key:X2}"] = GenerateKek(this["key_area_key_ocean_source"], masterKey.Value, this["aes_kek_generation_source"], this["aes_key_generation_source"]);
                if (!_keyMaterial.ContainsKey($"key_area_key_system_{masterKey.Key:X2}"))
                    this[$"key_area_key_system_{masterKey.Key:X2}"] = GenerateKek(this["key_area_key_system_source"], masterKey.Value, this["aes_kek_generation_source"], this["aes_key_generation_source"]);

                if (!_keyMaterial.ContainsKey($"titlekek_{masterKey.Key:X2}"))
                {
                    this[$"titlekek_{masterKey.Key:X2}"] = new byte[this["titlekek_source"].Length];
                    var ecb = new EcbStream(new MemoryStream(this["titlekek_source"]), masterKey.Value);
                    ecb.Read(this[$"titlekek_{masterKey.Key:X2}"], 0, this[$"titlekek_{masterKey.Key:X2}"].Length);
                }
            }

            KekApplication = new Dictionary<int, byte[]>(_keyMaterial
                .Where(x => Regex.IsMatch(x.Key, "key_area_key_application_[\\d]{2}"))
                .ToDictionary(x => Convert.ToInt32(x.Key.Replace("key_area_key_application_", "")), y => y.Value));
            KekOcean = new Dictionary<int, byte[]>(_keyMaterial
                .Where(x => Regex.IsMatch(x.Key, "key_area_key_ocean_[\\d]{2}"))
                .ToDictionary(x => Convert.ToInt32(x.Key.Replace("key_area_key_ocean_", "")), y => y.Value));
            KekSystem = new Dictionary<int, byte[]>(_keyMaterial
                .Where(x => Regex.IsMatch(x.Key, "key_area_key_system_[\\d]{2}"))
                .ToDictionary(x => Convert.ToInt32(x.Key.Replace("key_area_key_system_", "")), y => y.Value));
            TitleKek = new Dictionary<int, byte[]>(_keyMaterial
                .Where(x => Regex.IsMatch(x.Key, "titlekek_[\\d]{2}"))
                .ToDictionary(x => Convert.ToInt32(x.Key.Replace("titlekek_", "")), y => y.Value));
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
