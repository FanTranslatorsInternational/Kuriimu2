using System;

namespace Kontract.Attributes
{
    /// <summary>
    /// 
    /// </summary>
    public class MenuStripExtensionAttribute:Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public string[] Strips { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strips"></param>
        public MenuStripExtensionAttribute(params string[] strips)
        {
            Strips = strips;
        }
    }
}
