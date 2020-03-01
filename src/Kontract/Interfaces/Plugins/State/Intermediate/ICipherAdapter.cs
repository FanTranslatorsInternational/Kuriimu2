using System;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.Progress;
using Kontract.Models.Intermediate;

namespace Kontract.Interfaces.Plugins.State.Intermediate
{
    /// <summary>
    /// Provides methods to decrypt or encrypt files
    /// </summary>
    public interface ICipherAdapter : IIntermediate
    {
        /// <summary>
        /// Encrypts a file
        /// </summary>
        /// <param name="toEncrypt"></param>
        /// <param name="encryptInto"></param>
        /// <param name="progress"></param>
        Task<bool> Encrypt(Stream toEncrypt, Stream encryptInto, IProgressContext progress);

        /// <summary>
        /// Decrypts a file
        /// </summary>
        /// <param name="toDecrypt"></param>
        /// <param name="decryptInto"></param>
        /// <param name="progress"></param>
        Task<bool> Decrypt(Stream toDecrypt, Stream decryptInto, IProgressContext progress);
        
        /// <summary>
        /// Eventhandler for requesting data
        /// </summary>
        event EventHandler<RequestDataEventArgs> RequestData;
    }
}
