using System.Drawing;

namespace Kontract.Kanvas.Model
{
    /// <summary>
    /// The context class for encoding load operations.
    /// </summary>
    public class EncodingLoadContext
    {
        /// <summary>
        /// The degree of parallelism in the load operation.
        /// </summary>
        public int TaskCount { get; }

        /// <summary>
        /// The dimensions of the image to load.
        /// </summary>
        public Size Size { get; }

        public EncodingLoadContext(Size size, int taskCount)
        {
            TaskCount = taskCount;
            Size = size;
        }
    }
}
