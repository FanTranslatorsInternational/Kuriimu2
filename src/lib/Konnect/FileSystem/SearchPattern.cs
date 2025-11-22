// Copyright(c) 2017-2019, Alexandre Mutel
// All rights reserved.

// Redistribution and use in source and binary forms, with or without modification
// , are permitted provided that the following conditions are met:

// 1. Redistributions of source code must retain the above copyright notice, this
// list of conditions and the following disclaimer.

// 2. Redistributions in binary form must reproduce the above copyright notice,
// this list of conditions and the following disclaimer in the documentation
// and/or other materials provided with the distribution.

// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

// Modifications by onepiecefreak are as follows:
// - Matching whole path with .* instead of [/]* if * is given in the pattern

using System.Text;
using System.Text.RegularExpressions;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.FileSystem;
using Konnect.Extensions;

namespace Konnect.FileSystem;

/// <summary>
/// Search pattern compiler used for custom <see cref="IFileSystem.EnumeratePaths"/> implementations.
/// Use the method <see cref="Parse"/> to create a pattern.
/// </summary>
internal struct SearchPattern
{
    private static readonly char[] WildcardChars = { '?', '*' };

    private readonly string _exactMatch;
    private readonly Regex _regexMatch;

    /// <summary>
    /// Tries to match the specified path with this instance.
    /// </summary>
    /// <param name="path">The path to match.</param>
    /// <returns><c>true</c> if the path was matched, <c>false</c> otherwise.</returns>
    public bool Match(UPath path)
    {
        path.AssertNotNull();
        var name = path.FullName;
        // if _execMatch is null and _regexMatch is null, we have a * match
        return _exactMatch != null ? _exactMatch == name : _regexMatch == null || _regexMatch.IsMatch(name);
    }

    /// <summary>
    /// Tries to match the specified path with this instance.
    /// </summary>
    /// <param name="name">The path to match.</param>
    /// <returns><c>true</c> if the path was matched, <c>false</c> otherwise.</returns>
    public bool Match(string name)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        // if _execMatch is null and _regexMatch is null, we have a * match
        return _exactMatch != null ? _exactMatch == name : _regexMatch == null || _regexMatch.IsMatch(name);
    }

    /// <summary>
    /// Parses and normalize the specified path and <see cref="SearchPattern"/>.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="searchPattern">The search pattern.</param>
    /// <returns>An instance of <see cref="SearchPattern"/> in order to use <see cref="System.Text.RegularExpressions.Match"/> on a path.</returns>
    public static SearchPattern Parse(ref UPath path, ref string searchPattern)
    {
        return new SearchPattern(ref path, ref searchPattern);
    }

    /// <summary>
    /// Normalizes the specified path and <see cref="SearchPattern"/>.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="searchPattern">The search pattern.</param>
    public static void Normalize(ref UPath path, ref string searchPattern)
    {
        Parse(ref path, ref searchPattern);
    }

    private SearchPattern(ref UPath path, ref string searchPattern)
    {
        path.AssertAbsolute();
        if (searchPattern == null) throw new ArgumentNullException(nameof(searchPattern));

        _exactMatch = null;
        _regexMatch = null;

        //Optimized path, most common case
        if (searchPattern == "*" && path == UPath.Root)
        {
            return;
        }

        if (searchPattern.StartsWith("/"))
        {
            throw new ArgumentException($"The search pattern `{searchPattern}` cannot start by an absolute path `/`");
        }

        // Normalize path separators
        searchPattern = searchPattern.Replace('\\', '/');

        // If the path contains any directory, we need to concatenate the directory part with the input path (?)
        if (searchPattern.IndexOf('/') > 0)
        {
            var pathPattern = new UPath(searchPattern);
            var directory = pathPattern.GetDirectory();
            if (!directory.IsNull && !directory.IsEmpty)
            {
                path /= directory;
            }
            searchPattern = pathPattern.GetName();

            // If the search pattern is again a plain any, optimized path
            if (searchPattern == "*")
            {
                return;
            }
        }
            
        var regexBuilder = new StringBuilder("^");
        bool containsWildcards = false;
            
        // Loop through parts of searchPattern separated by wildcards
        for (int index = 0, nextWildcard = 0; nextWildcard != -1; index = nextWildcard + 1)
        {
            // Next wildcard occurence
            nextWildcard = searchPattern.IndexOfAny(WildcardChars, index);
                
            // Escape & append text up to next wildcard
            // If no new wildcard, append up to end of string
            var endOfPart = nextWildcard != -1 ? nextWildcard : searchPattern.Length;
            regexBuilder.Append(Regex.Escape(searchPattern.Substring(index, endOfPart - index)));

            // Convert & append wildcard, if applicable
            if (nextWildcard != -1)
            {
                var wc = searchPattern[nextWildcard];
                var regexPatternPart = wc switch
                {
                    '*' => ".*",
                    '?' => ".",
                    _ => throw new ArgumentException($"Unknown wildcard: {wc}")
                };
                regexBuilder.Append(regexPatternPart);
                containsWildcards = true;
            }
        }
            
        regexBuilder.Append("$");
            
        if (!containsWildcards)
        {
            _exactMatch = searchPattern;
        }
        else
        {
            _regexMatch = new Regex(regexBuilder.ToString());
        }
    }
}