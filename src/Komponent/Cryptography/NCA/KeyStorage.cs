using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komponent.Cryptography.AES;
using System.IO;
using System.Text.RegularExpressions;
using Komponent.Tools;

namespace Komponent.Cryptography.NCA
{
    public class KeyStorage
    {
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

        public KeyStorage(string keyFile)
        {
            if (!File.Exists(keyFile))
                throw new FileNotFoundException($"{keyFile} doesn't exist.");

            _keyMaterial = File.ReadAllLines(keyFile)
                .Select(l => l.Replace(" ", "").Replace("\t", ""))
                .Where(l => !l.StartsWith(";") && !String.IsNullOrEmpty(l) && Regex.IsMatch(l.Split('=').Skip(1).First(), "^[a-fA-F0-9]+$"))
                .ToDictionary(
                    l => l.Split('=').First(),
                    l => l.Split('=').Skip(1).First().Hexlify()
                    );

            var masterKeys = _keyMaterial.Where(k => Regex.IsMatch(k.Key, "master_key_[\\d]{2}")).Select(m => Convert.ToInt32(Regex.Match(m.Key, "[\\d]{2}").Value));
            foreach (var i in masterKeys)
            {
                if (!_keyMaterial.ContainsKey($"key_area_key_application_{i:00}"))
                    this[$"key_area_key_application_{i:00}"] = GenerateKek(this["key_area_key_application_source"], this[$"master_key_{i:00}"], this["aes_kek_generation_source"], this["aes_key_generation_source"]);
                if (!_keyMaterial.ContainsKey($"key_area_key_ocean_{i:00}"))
                    this[$"key_area_key_ocean_{i:00}"] = GenerateKek(this["key_area_key_ocean_source"], this[$"master_key_{i:00}"], this["aes_kek_generation_source"], this["aes_key_generation_source"]);
                if (!_keyMaterial.ContainsKey($"key_area_key_system_{i:00}"))
                    this[$"key_area_key_system_{i:00}"] = GenerateKek(this["key_area_key_system_source"], this[$"master_key_{i:00}"], this["aes_kek_generation_source"], this["aes_key_generation_source"]);

                if (!_keyMaterial.ContainsKey($"titlekek_{i:00}"))
                    new EcbStream(this["titlekek_source"], this[$"master_key_{i:00}"]).Read(this[$"titlekek_{i:00}"], 0, this[$"titlekek_{i:00}"].Length);
            }
        }

        private byte[] GenerateKek(byte[] generationSource, byte[] masterKey, byte[] aesKekGenSource, byte[] aesKeyGenSource)
        {
            var kek = new byte[16];
            var src_kek = new byte[16];

            new EcbStream(aesKekGenSource, masterKey).Read(kek, 0, kek.Length);
            new EcbStream(generationSource, kek).Read(src_kek, 0, src_kek.Length);

            if (aesKeyGenSource != null)
            {
                var buffer = new byte[16];
                new EcbStream(aesKeyGenSource, src_kek).Read(buffer, 0, buffer.Length);
                return buffer;
            }
            else
            {
                return src_kek;
            }
        }
    }
}
