using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Intermediate
{
    public class HashResult
    {
        public bool IsSuccessful { get; }
        public byte[] Result { get; }

        public HashResult(bool successful, byte[] hash)
        {
            IsSuccessful = successful;
            Result = hash;
        }
    }
}
