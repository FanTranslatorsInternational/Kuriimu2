using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.IndexEncoding.Models
{
    /// <summary>
    /// Base class for storing index data.
    /// </summary>
    public class IndexData
    {
        /// <summary>
        /// The value of the index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Creates a new instance of <see cref="IndexData"/>.
        /// </summary>
        /// <param name="index">The index value to be stored.</param>
        public IndexData(int index)
        {
            Index = index;
        }
    }
}
