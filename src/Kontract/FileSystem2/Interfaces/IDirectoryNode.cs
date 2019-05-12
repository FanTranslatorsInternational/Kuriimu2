using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.FileSystem2.Interfaces
{
    public interface IDirectoryNode<T> : INode<T>
    {
        /// <summary>
        /// The children nodes this directory node holds.
        /// </summary>
        IEnumerable<T> Children { get; }

        bool ContainsDirectory(string directory);
        bool ContainsFile(string file);

        IEnumerable<TDirectory> EnumerateDirectories<TDirectory>() where TDirectory : IDirectoryNode<T>;
        IEnumerable<TFile> EnumerateFiles<TFile>() where TFile : IFileNode<T>;

        TDirectory GetDirectory<TDirectory>(string relativePath) where TDirectory : IDirectoryNode<T>;
        TFile GetFile<TFile>(string relativePath) where TFile : IFileNode<T>;

        void Add(T entry);
        void AddRange(IEnumerable<T> entries);

        bool Remove(T entry);
    }
}
