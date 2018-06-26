using System.Drawing;
using System.IO;

namespace Kore.XFont.Interface
{
    public interface IFilePlugin
    {
        // Information
        string Name { get; } // i.e. return "KUP";
        string Description { get; } // i.e. return "Kuriimu Archive";
        string Extension { get; } // i.e. return "*.ext";
        //string Author { get; } // i.e. return "IcySon55";
        string About { get; } // i.e. return "This is the KUP text adapter for Kuriimu.";

        // Feature Support
        bool FileHasExtendedProperties { get; } // Format provides an extended properties dialog?
        //bool CanCreateNew { get; } // Is creating new files supported?
        bool CanSave { get; } // Is saving supported?

        // I/O
        FileInfo FileInfo { get; set; }
        bool Identify(string filename); // Determines if the given file is supported by the plugin.
        //void New();
        //void Load(string filename);
        //void Save(string filename = ""); // A non-blank filename is provided when using Save As...
        /*
         * Identification Identify(Stream file, string filename);
         * void Load(Stream file, string filename);
         * void Save(Stream file, string filename);
         * void New(Stream file, string filename)
         */

        // Features
        bool ShowProperties(Icon icon);
    }

    //public enum Identification
    //{
    //    True,
    //    False,
    //    Raw
    //}
}