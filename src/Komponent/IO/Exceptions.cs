using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komponent.IO
{
    public class FieldLengthMismatchException : Exception
    {
        public FieldLengthMismatchException(int given, int expected)
            : base($"The given length {given} of the object mismatches with the expected length {expected} of the field.")
        {
        }
    }

    public class UnsupportedTypeException : Exception
    {
        public UnsupportedTypeException(Type type)
            : base($"The given type {type.Name} is not supported.")
        {
        }
    }

    public class InvalidBitFieldInfoException : Exception
    {
        public InvalidBitFieldInfoException(int blockSize)
            : base($"The given BlockSize {blockSize} is not supported.")
        {
        }
    }
}
