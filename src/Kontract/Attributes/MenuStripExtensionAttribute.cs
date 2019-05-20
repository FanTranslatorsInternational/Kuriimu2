using System;

namespace Kontract.Attributes
{
    /// <summary>
    /// An attribute used to describe the path in a MenuStrip.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MenuStripExtensionAttribute : Attribute
    {
        /// <summary>
        /// An array containing the elements of the path in a MenuStrip.
        /// </summary>
        public string[] Strips { get; }

        /// <summary>
        /// Creates a new instance of <see cref="MenuStripExtensionAttribute"/>.
        /// </summary>
        /// <param name="strips">An array containing the elements of the path in a MenuStrip.</param>
        public MenuStripExtensionAttribute(params string[] strips)
        {
            Strips = strips;
        }
    }
}
