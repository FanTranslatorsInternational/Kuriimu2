﻿using System;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Managers
{
    /// <summary>
    /// Exposes methods to load files in the Kuriimu runtime.
    /// </summary>
    public interface IFileManager
    {
        #region Check

        bool IsLoading(UPath filePath);

        /// <summary>
        /// Determines if a file is already loaded.
        /// </summary>
        /// <param name="filePath">The path of the file to check.</param>
        /// <returns>If the file is already loaded.</returns>
        bool IsLoaded(UPath filePath);

        /// <summary>
        /// Determines if a state is currently saving.
        /// </summary>
        /// <param name="fileState">The state to check for saving.</param>
        /// <returns>If the state is currently saving.</returns>
        bool IsSaving(IFileState fileState);

        /// <summary>
        /// Determines is a state is currently closing.
        /// </summary>
        /// <param name="fileState">The state to check for closing.</param>
        /// <returns>If the state is currently closing.</returns>
        bool IsClosing(IFileState fileState);

        #endregion

        #region Load File

        #region Load FileSystem

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <returns>The loaded <see cref="IFileState"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path);

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="pluginId">The Id of the plugin to load the file with.</param>
        /// <returns>The loaded <see cref="IFileState"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId);

        /// <summary>
        /// Loads a file from a given file system.
        /// </summary>
        /// <param name="fileSystem">The file system to load the file from.</param>
        /// <param name="path">The file to load from the file system.</param>
        /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
        /// <returns>The loaded <see cref="IFileState"/> for the file.</returns>
        Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, LoadFileContext loadFileContext);

        #endregion

        #region Load ArchiveFileInfo

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="fileState">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(IFileState fileState, IArchiveFileInfo afi);

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="fileState">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <param name="pluginId">The plugin to load this virtual file with.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(IFileState fileState, IArchiveFileInfo afi, Guid pluginId);

        /// <summary>
        /// Loads a virtual path into the Kuriimu runtime.
        /// </summary>
        /// <param name="fileState">The loaded path state to load a path from.</param>
        /// <param name="afi">The path to load from that state.</param>
        /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(IFileState fileState, IArchiveFileInfo afi, LoadFileContext loadFileContext);

        #endregion

        #region Load Stream

        /// <summary>
        /// Loads a stream into the Kuriimu runtime.
        /// </summary>
        /// <param name="streamFile">The in-memory file to load.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(StreamFile streamFile);

        /// <summary>
        /// Loads a stream into the Kuriimu runtime.
        /// </summary>
        /// <param name="streamFile">The in-memory file to load.</param>
        /// <param name="pluginId">the plugin with which to load the file.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(StreamFile streamFile, Guid pluginId);

        /// <summary>
        /// Loads a stream into the Kuriimu runtime.
        /// </summary>
        /// <param name="streamFile">The in-memory file to load.</param>
        /// <param name="loadFileContext">The context with additional parameters for the load process.</param>
        /// <returns>The loaded state of the path.</returns>
        Task<LoadResult> LoadFile(StreamFile streamFile, LoadFileContext loadFileContext);

        #endregion

        #endregion

        #region Save File

        /// <summary>
        /// Save a loaded state in place.
        /// </summary>
        /// <param name="fileState">The <see cref="IFileState"/> to save.</param>
        /// <returns></returns>
        Task<SaveResult> SaveFile(IFileState fileState);

        /// <summary>
        /// Save a loaded state to the given filesystem and path.
        /// </summary>
        /// <param name="fileState">The <see cref="IFileState"/> to save.</param>
        /// <param name="fileSystem">The filesystem at which to save the file.</param>
        /// <param name="savePath">The path into the filesystem to save the file at.</param>
        /// <returns></returns>
        Task<SaveResult> SaveFile(IFileState fileState, IFileSystem fileSystem, UPath savePath);

        #endregion

        #region Save Stream

        /// <summary>
        /// Saves a loaded state into <see cref="StreamFile"/>s.
        /// </summary>
        /// <param name="fileState">The <see cref="IFileState"/> to save.</param>
        /// <returns></returns>
        Task<SaveStreamResult> SaveStream(IFileState fileState);

        #endregion

        #region Close File

        /// <summary>
        /// Closes a loaded state.
        /// </summary>
        /// <param name="fileState">The state to close and release.</param>
        CloseResult Close(IFileState fileState);

        /// <summary>
        /// Closes all loaded states.
        /// </summary>
        void CloseAll();

        #endregion
    }
}
