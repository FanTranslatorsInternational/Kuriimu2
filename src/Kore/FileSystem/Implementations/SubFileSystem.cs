// Copyright (c) 2017-2019, Alexandre Mutel
// All rights reserved.
// Modifications by onepiecefreak are as follows:
// - See main changes in the IFileSystem interface

using System;
using System.IO;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Models;
using Kontract.Models.IO;

namespace Kore.FileSystem.Implementations
{
    /// <summary>
    /// Provides a secure view on a sub folder of another delegate <see cref="IFileSystem"/>
    /// </summary>
    public class SubFileSystem : ComposeFileSystem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubFileSystem"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system to create a view from.</param>
        /// <param name="subPath">The sub path view to create filesystem.</param>
        /// <param name="owned">True if <paramref name="fileSystem"/> should be disposed when this instance is disposed.</param>
        /// <exception cref="DirectoryNotFoundException">If the directory subPath does not exist in the delegate FileSystem</exception>
        public SubFileSystem(IFileSystem fileSystem, UPath subPath, bool owned = true) : base(fileSystem, owned)
        {
            SubPath = subPath.AssertAbsolute(nameof(subPath));
            if (!fileSystem.DirectoryExists(SubPath))
            {
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(SubPath);
            }
        }

        /// <inheritdoc />
        public override IFileSystem Clone(IStreamManager streamManager)
        {
            var clonedFs = base.Clone(streamManager);
            return new SubFileSystem(clonedFs, SubPath, Owned);
        }

        /// <summary>
        /// Gets the sub path relative to the delegate <see cref="ComposeFileSystem.NextFileSystem"/>
        /// </summary>
        public UPath SubPath { get; }

        /// <inheritdoc />
        protected override UPath ConvertPathToDelegate(UPath path)
        {
            var safePath = path.ToRelative();
            return SubPath / safePath;
        }

        /// <inheritdoc />
        protected override UPath ConvertPathFromDelegate(UPath path)
        {
            var fullPath = path.FullName;
            if (!fullPath.StartsWith(SubPath.FullName) || (fullPath.Length > SubPath.FullName.Length && fullPath[SubPath == UPath.Root ? 0 : SubPath.FullName.Length] != UPath.DirectorySeparator))
            {
                // More a safe guard, as it should never happen, but if a delegate filesystem doesn't respect its root path
                // we are throwing an exception here
                throw new InvalidOperationException($"The path `{path}` returned by the delegate filesystem is not rooted to the subpath `{SubPath}`");
            }

            var subPath = fullPath.Substring(SubPath.FullName.Length);
            return subPath == string.Empty ? UPath.Root : new UPath(subPath, true);
        }
    }
}
