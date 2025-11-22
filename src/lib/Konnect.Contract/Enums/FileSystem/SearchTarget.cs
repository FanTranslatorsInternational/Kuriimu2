// Copyright (c) 2017-2019, Alexandre Mutel
// All rights reserved.

using Konnect.Contract.FileSystem;

namespace Konnect.Contract.Enums.FileSystem;

/// <summary>
/// Defines the behavior of <see cref="IFileSystem.EnumeratePaths"/> when looking for files and/or folders.
/// </summary>
public enum SearchTarget
{
    /// <summary>
    /// Search for both files and folders.
    /// </summary>
    Both,

    /// <summary>
    /// Search for files.
    /// </summary>
    File,

    /// <summary>
    /// Search for directories.
    /// </summary>
    Directory
}