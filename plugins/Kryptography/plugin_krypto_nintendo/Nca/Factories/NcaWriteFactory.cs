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
    /// Factory to configure cipher information for write operations on an NCA
    /// </summary>
    public class NcaWriteFactory
    {
        private NcaKeyStorage _keyStorage;

        /// <summary>
        /// Creates a factory for write operations, based on the information identified by the given decrypted NCA
        /// </summary>
        /// <param name="decryptedNca">The decrypted nca to identify cipher information from</param>
        public NcaWriteFactory(Stream decryptedNca)
        {
            // TODO: Identify from stream
        }

        /// <summary>
        /// Creates a factory for write operations, based on given cipher information
        /// </summary>
        /// <param name="version">The version of the NCA</param>
        /// <param name="masterKeyRevision">The master key revision to be used</param>
        /// <param name="keyFile">The file containing all keys for cipher operations</param>
        /// <param name="sections">the NCA body section information</param>
        public NcaWriteFactory(NcaVersion version, int masterKeyRevision, string keyFile, params NcaBodySection[] sections)
        {
            if (masterKeyRevision < 0 || masterKeyRevision > 31)
                throw new InvalidOperationException($"Invalid master key revision.");
            if (sections.Length > 4)
                throw new InvalidOperationException($"Only 4 sections are allowed at max.");
            if (string.IsNullOrEmpty(keyFile))
                throw new ArgumentNullException(nameof(keyFile));

            SetKeyFile(keyFile);

            NcaVersion = version;
            MasterKeyRevision = masterKeyRevision;
            Sections = sections;
        }

        /// <summary>
        /// Sets the key storage to the given file
        /// </summary>
        /// <param name="keyFile">File containing all keys for cipher operations</param>
        public void SetKeyFile(string keyFile)
        {
            _keyStorage = new NcaKeyStorage(keyFile);
        }

        /// <summary>
        /// The version of the NCA
        /// </summary>
        public NcaVersion NcaVersion { get; }

        /// <summary>
        /// The master key revision to be used for key area key and title key decryption
        /// </summary>
        public int MasterKeyRevision { get; }

        /// <summary>
        /// Information on each body section in the NCA
        /// </summary>
        public NcaBodySection[] Sections { get; }

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
        /// Creates a writable stream based on the given cipher information in this factory
        /// </summary>
        /// <param name="writeStream">The stream to be written to</param>
        /// <returns>The writable stream for the NCA</returns>
        public Stream CreateWritableStream(Stream writeStream)
        {
            byte[] decTitleKey = null;
            byte[] decKeyArea = null;
            if (UseTitleKeyEncryption)
            {
                if (EncryptedTitleKey == null)
                    throw new ArgumentNullException(nameof(EncryptedTitleKey));
                if (EncryptedTitleKey.Length != 0x10)
                    throw new InvalidOperationException($"Invalid title key length.");

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

            // TODO: Refactor readable stream to take in sections; To be writable
            var stream = new NcaReadableStream(writeStream, NcaVersion, decKeyArea, _keyStorage, decTitleKey, true);
            
            return stream;
        }
    }
}
