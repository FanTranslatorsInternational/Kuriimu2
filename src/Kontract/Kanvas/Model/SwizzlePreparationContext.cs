using System.Drawing;
using Kontract.Kanvas.Configuration;

namespace Kontract.Kanvas.Model
{
    public class SwizzlePreparationContext
    {
        /// <summary>
        /// The meta information of the encoding used.
        /// </summary>
        public IEncodingInfo EncodingInfo { get; }

        /// <summary>
        /// The size of the image.<para></para>
        /// If <see cref="IImageConfiguration.PadSize"/> is specified, this is the padded size.
        /// </summary>
        public Size Size { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SwizzlePreparationContext"/>.
        /// </summary>
        /// <param name="encodingInfo">The meta information for the current encoding.</param>
        /// <param name="size">The exact or padded size of the image.</param>
        public SwizzlePreparationContext(IEncodingInfo encodingInfo, Size size)
        {
            EncodingInfo = encodingInfo;
            Size = size;
        }
    }
}
