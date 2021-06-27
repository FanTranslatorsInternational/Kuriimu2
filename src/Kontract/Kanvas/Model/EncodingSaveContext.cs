using System.Drawing;

namespace Kontract.Kanvas.Model
{
    /// <summary>
    /// The context class for encoding save operations.
    /// </summary>
    public class EncodingSaveContext
    {
        /// <summary>
        /// The degree of parallelism in the load operation.
        /// </summary>
        public int TaskCount { get; }

        /// <summary>
        /// The dimensions of the image to save.
        /// </summary>
        public Size Size { get; }

        public EncodingSaveContext(Size size, int taskCount)
        {
            TaskCount = taskCount;
            Size = size;
        }
    }
}
