using Komponent.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca.KeyStorages
{
    internal class NcaTitleKeyStorage
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

        public NcaTitleKeyStorage(string titleKeyFile)
        {
            if (!File.Exists(titleKeyFile))
                throw new FileNotFoundException(titleKeyFile);

            LoadTitleKeys(titleKeyFile);
        }

        private void LoadTitleKeys(string titleKeyFile)
        {
            _keyMaterial = File.ReadAllLines(titleKeyFile)
                .Select(l => l.Replace(" ", "").Replace("\t", ""))
                .Where(l => !l.StartsWith(";") &&
                    !string.IsNullOrEmpty(l) &&
                    Regex.IsMatch(l.Split('=').First(), "^[a-fA-F0-9]+$") &&
                    Regex.IsMatch(l.Split('=').Skip(1).First(), "^[a-fA-F0-9]+$"))
                .ToDictionary(
                    l => l.Split('=').First(),
                    l => l.Split('=').Skip(1).First().Hexlify()
                );

            foreach (var key in _keyMaterial)
            {
                if (key.Key.Length != 8)
                    throw new InvalidOperationException($"Title id \"{key.Key}\" has an invalid length.");
                if (key.Value.Length != 0x10)
                    throw new InvalidOperationException($"Encrypted title key at title id \"{key.Key}\" has an invalid length.");
            }

        }

        public bool Contains(string key) => _keyMaterial.ContainsKey(key);
    }
}
