using plugin_krypto_nintendo.Nca.KeyStorages;
using plugin_krypto_nintendo.Nca.Models;
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
        /// <param name="baseStream">The stream to be written to</param>
        /// <returns>The writable stream for the NCA</returns>
        public Stream CreateWritableStream(Stream baseStream)
        {
            // TODO: check each property for validity
            // TODO: Create writable stream; Maybe refactor NcaReadableStream to do both
            return null;
        }
    }
}
