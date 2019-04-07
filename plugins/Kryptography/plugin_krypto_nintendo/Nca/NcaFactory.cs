using Komponent.IO;
using Kryptography.AES;
using plugin_krypto_nintendo.Nca.KeyStorages;
using plugin_krypto_nintendo.Nca.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca
{
    public class NcaFactory
    {
        private Stream _baseStream;
        private NcaKeyStorage _keyStorage;
        private NcaTitleKeyStorage _titleKeyStorage;
        private byte[] _rightsId;
        private bool _setTitleKey;
        private byte[] _encTitleKey;

        public NcaVersion NcaVersion { get; set; }
        public int MasterKeyRev { get; set; }
        public bool IsEncrypted { get; set; }
        public byte[] DecryptedKeyArea { get; set; }
        public bool HasRightsId { get; set; }
        public byte[] EncryptedTitleKey
        {
            get => _encTitleKey;
            set
            {
                _encTitleKey = value;
                _setTitleKey = value != null;
            }
        }

        public NcaFactory(Stream nca, string keyFile) : this(nca, keyFile, null) { }

        public NcaFactory(Stream nca, string keyFile, string titleKeyFile)
        {
            if (nca == null)
                throw new ArgumentNullException(nameof(nca));
            if (nca.Length < NcaConstants.HeaderSize)
                throw new InvalidOperationException("Stream is too short.");
            if (string.IsNullOrEmpty(keyFile))
                throw new ArgumentNullException(nameof(keyFile));

            SetKeyFile(keyFile);
            SetTitleKeyFile(titleKeyFile);

            IdentifyInformation(nca);

            _baseStream = nca;
        }

        private void IdentifyInformation(Stream nca)
        {
            var bkPos = nca.Position;

            nca.Position = 0x200;
            var magic = new byte[4];
            nca.Read(magic, 0, 4);
            nca.Position = bkPos;
            
            if (!Enum.TryParse<NcaVersion>(Encoding.ASCII.GetString(magic), out var ver))
            {
                IsEncrypted = true;
                var xts = new XtsStream(nca, _keyStorage.HeaderKey, new byte[0x10], true, false, NcaConstants.MediaSize);
                xts.Position = 0x200;
                xts.Read(magic, 0, 4);

                if (!Enum.TryParse(Encoding.ASCII.GetString(magic), out ver))
                    throw new InvalidOperationException("No valid Nca.");
            }

            NcaVersion = ver;
            var ncaHeader = new NcaHeaderStream(new SubStream(nca, 0, NcaConstants.HeaderSize), ver, _keyStorage.HeaderKey, IsEncrypted);

            IdentifyMasterKey(ncaHeader);
            IdentifyKeyArea(ncaHeader);
            IdentifyRightsId(ncaHeader);
        }

        private void IdentifyMasterKey(Stream headerNca)
        {
            headerNca.Position = 0x206;
            var type1 = new byte[1];
            headerNca.Read(type1, 0, 1);

            headerNca.Position = 0x220;
            var type2 = new byte[1];
            headerNca.Read(type2, 0, 1);

            var cryptoType = Math.Max(type2[0], type1[0]);
            if (cryptoType >= 1) cryptoType--;
            MasterKeyRev = cryptoType;
        }

        private void IdentifyKeyArea(Stream headerNca)
        {
            headerNca.Position = 0x207;
            var keyIndex = new byte[1];
            headerNca.Read(keyIndex, 0, 1);

            byte[] decKey = null;
            switch (keyIndex[0])
            {
                case 0:
                    decKey = _keyStorage[$"key_area_key_application_{MasterKeyRev:00}"];
                    break;
                case 1:
                    decKey = _keyStorage[$"key_area_key_ocean_{MasterKeyRev:00}"];
                    break;
                case 2:
                    decKey = _keyStorage[$"key_area_key_system_{MasterKeyRev:00}"];
                    break;
            }

            DecryptedKeyArea = new byte[0x40];
            headerNca.Position = 0x300;
            headerNca.Read(DecryptedKeyArea, 0, 0x40);
            new EcbStream(new MemoryStream(DecryptedKeyArea), decKey).Read(DecryptedKeyArea, 0, 0x40);
        }

        private void IdentifyRightsId(Stream headerNca)
        {
            headerNca.Position = 0x230;
            _rightsId = new byte[0x10];
            headerNca.Read(_rightsId, 0, 0x10);

            for (int i = 0; i < 0x10; i++)
                if (_rightsId[i] != 0)
                {
                    HasRightsId = true;
                    break;
                }
        }

        public void SetKeyFile(string keyFile)
        {
            _keyStorage = new NcaKeyStorage(keyFile);
        }

        public void SetTitleKeyFile(string titleKeyFile)
        {
            if (string.IsNullOrEmpty(titleKeyFile))
            {
                _titleKeyStorage = null;
                return;
            }

            _titleKeyStorage = new NcaTitleKeyStorage(titleKeyFile);
        }

        public Stream CreateReadableStream()
        {
            byte[] titleKey = null;
            if (HasRightsId)
            {
                titleKey = new byte[0x10];
                if (_setTitleKey && _encTitleKey.Length != 0x10)
                    throw new InvalidOperationException($"Invalid title key length.");

                if (_setTitleKey)
                    Array.Copy(_encTitleKey, titleKey, 0x10);
                else
                {
                    if (_titleKeyStorage == null)
                        throw new ArgumentNullException(nameof(_titleKeyStorage));
                    if (!_titleKeyStorage.Contains(_rightsId.Stringify()))
                        throw new InvalidOperationException($"No title key found for title \"{_rightsId.Stringify()}\"");
                    Array.Copy(_titleKeyStorage[_rightsId.Stringify()], titleKey, 0x10);
                }

                if (_keyStorage.TitleKek.ContainsKey(MasterKeyRev))
                    new EcbStream(new MemoryStream(titleKey), _keyStorage.TitleKek[MasterKeyRev]);
            }

            if (!Enum.IsDefined(typeof(NcaVersion), (int)NcaVersion))
                throw new ArgumentException(nameof(NcaVersion));

            if (!_keyStorage.MasterKeys.ContainsKey(MasterKeyRev))
                throw new InvalidOperationException($"Masterkey {MasterKeyRev} was not found.");

            if (DecryptedKeyArea == null)
                throw new ArgumentNullException(nameof(DecryptedKeyArea));
            if (DecryptedKeyArea.Length != 0x40)
                throw new InvalidOperationException($"Key area has to be 0x40 bytes.");

            return new NcaReadableStream(_baseStream, NcaVersion, DecryptedKeyArea, _keyStorage, titleKey, IsEncrypted);
        }

        // TODO: Implement creation of writable stream
        //public NcaWritableStream CreateWritableStream(Stream ncaFile)
        //{

        //}
    }
}
