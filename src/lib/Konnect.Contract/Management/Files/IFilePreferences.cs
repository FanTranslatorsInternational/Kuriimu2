using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Konnect.Contract.DataClasses.Management.Files;

namespace Konnect.Contract.Management.Files
{
    public interface IFilePreferences
    {
        string[] GetPaths();

        FilePreferenceEntry? GetOrDefault(string fullPath);

        void Set(string fullPath, FilePreferenceEntry entry);

        void Remove(string filePath);
    }
}
