using System;

namespace Komponent.Exceptions
{
    public class InvalidBitFieldInfoException : Exception
    {
        public InvalidBitFieldInfoException(int blockSize)
            : base($"The given BlockSize {blockSize} is not supported.")
        {
        }
    }
}
