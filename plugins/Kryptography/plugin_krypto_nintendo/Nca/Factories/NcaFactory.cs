using Komponent.IO;
using Kryptography.AES;
using plugin_krypto_nintendo.Nca.KeyStorages;
using plugin_krypto_nintendo.Nca.Models;
using plugin_krypto_nintendo.Nca.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca.Factories
{
    /// <summary>
    /// Factory to configure settings for cipher operations on an NCA
    /// </summary>
    public class NcaFactory
    {
        private NcaKeyStorage _keyStorage;
        private NcaTitleKeyStorage _titleKeyStorage;

        /// <summary>
        /// The version of the NCA
        /// </summary>
        public NcaVersion NcaVersion { get; private set; }

        /// <summary>
        /// The master key revision to be used for key area key and title key decryption
        /// </summary>
        public int MasterKeyRevision { get; private set; }

        /// <summary>
        /// Information on each body section in the NCA
        /// </summary>
        public NcaBodySection[] Sections { get; private set; }

        /// <summary>
        /// The key index to decrypt the key area
        /// </summary>
        public KeyAreaKeyType KeyAreaKeyType { get; set; }

        /// <summary>
        /// The encrypted key area to be used for body section cipher operations
        /// </summary>
        public byte[] EncryptedKeyArea { get; set; }

        /// <summary>
        /// Defines if the title key encryption should be used
        /// </summary>
        public bool UseTitleKeyEncryption { get; set; }

        /// <summary>
        /// The encrypted title key to be used for body section cipher operations
        /// </summary>
        public byte[] EncryptedTitleKey { get; set; }

        /// <summary>
        /// The title id for autodetecting encrypted title key
        /// </summary>
        public byte[] TitleId { get; set; }

        /// <summary>
        /// Creates a factory for write operations, based on the information identified by the given NCA
        /// </summary>
        /// <param name="nca">The nca to identify cipher information from</param>
        /// <param name="keyFile">The file containing all keys for cipher operations</param>
        public NcaFactory(Stream nca, string keyFile) : this(nca, keyFile, null)
        {
        }

        /// <summary>
        /// Creates a factory for write operations, based on the information identified by the given NCA
        /// </summary>
        /// <param name="nca">The nca to identify cipher information from</param>
        /// <param name="keyFile">The file containing all keys for cipher operations</param>
        /// <param name="titleKeyFile">The file containing all title keys for cipher operations</param>
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
        }

        /// <summary>
        /// Creates a factory for write operations, based on given cipher information
        /// </summary>
        /// <param name="version">The version of the NCA</param>
        /// <param name="masterKeyRevision">The master key revision to be used</param>
        /// <param name="keyFile">The file containing all keys for cipher operations</param>
        /// <param name="sections">the NCA body section information</param>
        public NcaFactory(NcaVersion version, int masterKeyRevision, string keyFile, params NcaBodySection[] sections)
        {
            if (masterKeyRevision < 0 || masterKeyRevision > 31)
                throw new InvalidOperationException($"Invalid master key revision.");
            if (sections.Length > 4)
                throw new InvalidOperationException($"Only 4 sections are allowed at most.");
            if (string.IsNullOrEmpty(keyFile))
                throw new ArgumentNullException(nameof(keyFile));

            SetKeyFile(keyFile);

            NcaVersion = version;
            MasterKeyRevision = masterKeyRevision;
            Sections = sections;
        }

        /// <summary>
        /// Sets the key storage
        /// </summary>
        /// <param name="keyFile">File containing all keys for cipher operations</param>
        public void SetKeyFile(string keyFile)
        {
            _keyStorage = new NcaKeyStorage(keyFile);
        }

        /// <summary>
        /// Sets the title key storage
        /// </summary>
        /// <param name="titleKeyFile">File containing all title keys for cipher operations</param>
        public void SetTitleKeyFile(string titleKeyFile)
        {
            if (string.IsNullOrEmpty(titleKeyFile))
            {
                _titleKeyStorage = null;
                return;
            }

            _titleKeyStorage = new NcaTitleKeyStorage(titleKeyFile);
        }

        private void IdentifyInformation(Stream nca)
        {
            var bkPos = nca.Position;

            nca.Position = 0x200;
            var magic = new byte[4];
            nca.Read(magic, 0, 4);
            nca.Position = bkPos;

            var header = nca;
            if (!Enum.TryParse<NcaVersion>(Encoding.ASCII.GetString(magic), out var ver))
            {
                var xts = new XtsStream(nca, _keyStorage.HeaderKey, new byte[0x10], true, false, NcaConstants.MediaSize)
                {
                    Position = 0x200
                };
                xts.Read(magic, 0, 4);

                if (!Enum.TryParse(Encoding.ASCII.GetString(magic), out ver))
                    throw new InvalidOperationException("No valid Nca.");

                header = new NcaHeaderStream(nca, ver, _keyStorage.HeaderKey);
            }

            NcaVersion = ver;

            IdentifyMasterKeyRevision(header);
            IdentifyEncryptedKeyArea(header);
            IdentifyEncryptedTitleKey(header);
            IdentifyTitleId(header);
            IdentifyBodySections(header);
            header.Position = 0;
        }

        private void IdentifyMasterKeyRevision(Stream header)
        {
            header.Position = 0x206;
            var type1 = new byte[1];
            header.Read(type1, 0, 1);

            header.Position = 0x220;
            var type2 = new byte[1];
            header.Read(type2, 0, 1);

            var cryptoType = Math.Max(type2[0], type1[0]);
            if (cryptoType >= 1) cryptoType--;
            MasterKeyRevision = cryptoType;
        }

        private void IdentifyEncryptedKeyArea(Stream header)
        {
            header.Position = 0x300;
            EncryptedKeyArea = new byte[0x40];
            header.Read(EncryptedKeyArea, 0, 0x40);
        }

        private void IdentifyEncryptedTitleKey(Stream header)
        {
            var rightsId = new byte[0x10];
            header.Position = 0x230;
            header.Read(rightsId, 0, 0x10);

            UseTitleKeyEncryption = false;
            for (int i = 0; i < 0x10; i++)
                if (rightsId[i] != 0)
                {
                    UseTitleKeyEncryption = true;
                    break;
                }

            if (UseTitleKeyEncryption && _titleKeyStorage != null)
            {
                EncryptedTitleKey = new byte[0x10];
                Array.Copy(_titleKeyStorage[rightsId.Stringify()], EncryptedTitleKey, 0x10);
            }
        }

        private void IdentifyTitleId(Stream header)
        {
            header.Position = 0x230;
            TitleId = new byte[0x10];
            header.Read(TitleId, 0, 0x10);
        }

        private void IdentifyBodySections(Stream header)
        {
            header.Position = 0x240;
            var sections = new byte[0x40];
            header.Read(sections, 0, 0x40);

            var bodySections = new List<NcaBodySection>();
            for (int i = 0; i < 4; i++)
            {
                var mediaOffset = BitConverter.ToInt32(sections, i * 0x10);
                var mediaEndOffset = BitConverter.ToInt32(sections, i * 0x10 + 4);
                if (mediaOffset == 0 || mediaEndOffset == 0)
                    continue;

                var sectionHeaderOffset = NcaConstants.HeaderWithoutSectionsSize + i * NcaConstants.MediaSize;
                header.Position = sectionHeaderOffset + 4;
                var sectionCrypto = new byte[1];
                header.Read(sectionCrypto, 0, 1);
                if (sectionCrypto[0] < 1 || sectionCrypto[0] > 4)
                    throw new InvalidOperationException($"CryptoType for section {i} must be 1-4. Found CryptoType: {sectionCrypto[0]}");

                var sectionCtr = new byte[8];
                header.Position = sectionHeaderOffset + 0x140;
                header.Read(sectionCtr, 0, 8);
                sectionCtr = sectionCtr.Reverse().ToArray();
                var sectionIv = new byte[0x10];
                Array.Copy(sectionCtr, sectionIv, 8);

                bodySections.Add(new NcaBodySection(mediaOffset, mediaEndOffset - mediaOffset, (NcaSectionCrypto)sectionCrypto[0], sectionIv));
            }

            Sections = bodySections.ToArray();
        }

        /// <summary>
        /// Creates a stream based on the given cipher settings in this factory
        /// </summary>
        /// <param name="baseStream">The base stream</param>
        /// <returns>The crypto stream for the NCA</returns>
        /// <remarks>If <see cref="UseTitleKeyEncryption"/> is set, every section crypto type gets set to title key encryption;
        /// Otherwise if <see cref="UseTitleKeyEncryption"/> is not set and section crypto type is title key, section crypto type gets set to ctr encryption</remarks>
        public Stream CreateStream(Stream baseStream)
        {
            foreach (var section in Sections)
            {
                if (UseTitleKeyEncryption && section.SectionCrypto == NcaSectionCrypto.Ctr)
                    section.SectionCrypto = NcaSectionCrypto.TitleKey;
                else
                    if (section.SectionCrypto == NcaSectionCrypto.TitleKey)
                    section.SectionCrypto = NcaSectionCrypto.Ctr;
            }

            byte[] decTitleKey = null;
            byte[] decKeyArea = null;
            if (UseTitleKeyEncryption)
            {
                if (EncryptedTitleKey == null && _titleKeyStorage == null)
                    throw new InvalidOperationException("Title key storage isn't set and no encrypted title key was given.");
                if (EncryptedTitleKey == null)
                {
                    if (TitleId.Length != 0x10)
                        throw new InvalidOperationException("Title id has invalid length.");
                    if (!_titleKeyStorage.Contains(TitleId.Stringify()))
                        throw new InvalidOperationException("Title id was not found in title key storage.");

                    EncryptedTitleKey = new byte[0x10];
                    Array.Copy(_titleKeyStorage[TitleId.Stringify()], EncryptedTitleKey, 0x10);
                }
                else
                {
                    if (EncryptedTitleKey.Length != 0x10)
                        throw new InvalidOperationException($"Invalid title key length.");
                }

                // Decrypt title key
                if (!_keyStorage.TitleKek.ContainsKey(MasterKeyRevision))
                    throw new InvalidOperationException($"No title kek found for master key revision {MasterKeyRevision}.");

                decTitleKey = new byte[0x10];
                new EcbStream(new MemoryStream(EncryptedTitleKey), _keyStorage.TitleKek[MasterKeyRevision]).Read(decTitleKey, 0, decTitleKey.Length); ;
            }
            else
            {
                if (EncryptedKeyArea == null)
                    throw new ArgumentNullException(nameof(EncryptedKeyArea));
                if (EncryptedKeyArea.Length != 0x40)
                    throw new InvalidOperationException($"Invalid key area length.");

                // Decrypt key area
                decKeyArea = new byte[0x40];
                byte[] decKey;
                switch (KeyAreaKeyType)
                {
                    case KeyAreaKeyType.Application:
                        decKey = _keyStorage[$"key_area_key_application_{MasterKeyRevision:00}"];
                        break;
                    case KeyAreaKeyType.Ocean:
                        decKey = _keyStorage[$"key_area_key_ocean_{MasterKeyRevision:00}"];
                        break;
                    case KeyAreaKeyType.System:
                        decKey = _keyStorage[$"key_area_key_system_{MasterKeyRevision:00}"];
                        break;
                    default:
                        throw new InvalidOperationException($"KeyAreaType {KeyAreaKeyType} not supported.");
                }

                new EcbStream(new MemoryStream(EncryptedKeyArea), decKey).Read(decKeyArea, 0, decKeyArea.Length);
            }

            return new NcaStream(baseStream, NcaVersion, Sections, _keyStorage, decKeyArea, decTitleKey);
        }
    }
}
