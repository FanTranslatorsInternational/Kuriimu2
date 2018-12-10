namespace Kontract.Interfaces
{
    /// <summary>
    /// Allows a format to implement format conversion.
    /// </summary>
    public interface IFormatConverter<in T, out TResult>
    {
        /// <summary>
        /// Converts a source format to the destination format.
        /// </summary>
        /// <typeparam name="T">The source type to convert from.</typeparam>
        /// <typeparam name="TResult">The destination type to convert to.</typeparam>
        /// <param name="inFormat">An instance of the source format.</param>
        /// <returns>The a new instance of the destination format.</returns>
        TResult ConvertTo(T inFormat);
    }
}
