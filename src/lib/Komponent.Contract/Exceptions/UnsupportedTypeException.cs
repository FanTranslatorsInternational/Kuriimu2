namespace Komponent.Contract.Exceptions
{
    public class UnsupportedTypeException(Type type) : Exception($"The given type {type.Name} is not supported.");
}
