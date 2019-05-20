using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Models;

namespace Kontract.Interfaces.Intermediate
{
    /// <summary>
    /// Provides methods to hash files.
    /// </summary>
    public interface IHashAdapter:IIntermediate
    {
        /// <summary>
        /// Compute a hash from a file.
        /// </summary>
        /// <param name="toHash"></param>
        /// <param name="hashInto"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        Task<bool> Compute(Stream toHash, Stream hashInto, IProgress<ProgressReport> progress);
    }
}
