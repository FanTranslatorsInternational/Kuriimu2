using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            LoadTitleKeys();
        }

        // TODO: Implement TitleKey loading
        private void LoadTitleKeys()
        {

        }

        public bool Contains(string key) => _keyMaterial.ContainsKey(key);
    }
}
