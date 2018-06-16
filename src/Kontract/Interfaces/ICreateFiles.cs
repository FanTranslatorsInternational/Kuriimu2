namespace Kontract.Interfaces
{
    /// <summary>
    /// This interface allows a plugin to create files.
    /// </summary>
    public interface ICreateFiles
    {
        /// <summary>
        /// Creates a new instance of the underlying format.
        /// </summary>
        void Create();
    }
}
