using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kore.Utilities.Models;

namespace Kore.Utilities.Text
{
    /// <summary>
    /// The main implementation for text sequence searching.
    /// </summary>
    public class TextSequenceSearcher
    {
        /// <summary>
        /// The Knuth-Morris-Pratt String pattern searcher
        /// </summary>
        private KmpSearcher _kmpSearcher;

        /// <summary>
        /// The directory to search in.
        /// </summary>
        public string SearchDirectory { get; }

        /// <summary>
        /// The max size of files to search through.
        /// </summary>
        public int FileSizeLimitInBytes { get; }

        /// <summary>
        /// Will sub directories be searched too.
        /// </summary>
        public bool IsSearchSubDirectories { get; set; }

        /// <summary>
        /// The encoding used to search the pattern.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.ASCII;

        /// <summary>
        /// Creates a new instance of <see cref="TextSequenceSearcher"/>.
        /// </summary>
        /// <param name="searchDirectory">The directory to search in.</param>
        /// <param name="fileSizeLimitInBytes">The max size of files to search through.</param>
        public TextSequenceSearcher(string searchDirectory, int fileSizeLimitInBytes)
        {
            if (string.IsNullOrEmpty(searchDirectory))
                throw new ArgumentException(nameof(searchDirectory));
            if (!Directory.Exists(searchDirectory))
                throw new DirectoryNotFoundException(searchDirectory);
            if (fileSizeLimitInBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(fileSizeLimitInBytes));

            SearchDirectory = searchDirectory;
            FileSizeLimitInBytes = fileSizeLimitInBytes;
        }

        public IEnumerable<SequenceSearchResult> Search(string toFind)
        {
            _kmpSearcher = new KmpSearcher(Encoding.GetBytes(toFind));

            return SearchInDirectory(SearchDirectory);
        }

        private IEnumerable<SequenceSearchResult> SearchInDirectory(string directory)
        {
            foreach (var file in Directory.EnumerateFiles(SearchDirectory))
            {
                if (new FileInfo(file).Length > FileSizeLimitInBytes)
                    continue;

                // Search string in file
                var foundOffset = _kmpSearcher.Search(new BinaryReader(File.OpenRead(file)));
                if (foundOffset >= 0)
                    yield return new SequenceSearchResult(file, foundOffset);
            }

            if (!IsSearchSubDirectories)
                yield break;

            foreach (var dir in Directory.EnumerateDirectories(directory))
            {
                foreach (var element in SearchInDirectory(dir))
                {
                    yield return element;
                }
            }
        }
    }
}
