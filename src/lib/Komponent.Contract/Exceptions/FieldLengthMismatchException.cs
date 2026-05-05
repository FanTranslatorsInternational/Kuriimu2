namespace Komponent.Contract.Exceptions
{
    public class FieldLengthMismatchException(int given, int expected)
        : Exception($"The given length {given} of the object mismatches with the expected length {expected} of the field.");
}
