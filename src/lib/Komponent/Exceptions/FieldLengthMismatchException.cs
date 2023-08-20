using System;

namespace Komponent.Exceptions
{
    public class FieldLengthMismatchException : Exception
    {
        public FieldLengthMismatchException(int given, int expected)
            : base($"The given length {given} of the object mismatches with the expected length {expected} of the field.")
        {
        }
    }
}
