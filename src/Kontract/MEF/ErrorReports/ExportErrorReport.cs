#if !NET_CORE_21
using System;
using System.ComponentModel.Composition.Primitives;
using Kontract.MEF.Interfaces;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Kontract.MEF.ErrorReports
{
    /// <summary>
    /// Error report containing specific definition information on the failed import and export.
    /// </summary>
    public class ExportErrorReport : IErrorReport
    {
        /// <inheritdoc cref="IErrorReport.Exception"/>
        public Exception Exception { get; }

        /// <summary>
        /// Import definition the erro was thrown at.
        /// </summary>
        public ImportDefinition ImportDefinition { get; }

        /// <summary>
        /// Export definition that was tried to be applied.
        /// </summary>
        public ExportDefinition ExportDefinition { get; }

        /// <summary>
        /// The composable part that was tried to import.
        /// </summary>
        public ComposablePartDefinition ComposablePartDefinition { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ExportErrorReport"/>.
        /// </summary>
        /// <param name="ex">Exception thrown on import.</param>
        /// <param name="import">Import definition the error was thrown at.</param>
        /// <param name="export">Export definition tried to apply.</param>
        /// <param name="composable">The composable part tried to import.</param>
        public ExportErrorReport(Exception ex, ImportDefinition import, ExportDefinition export, ComposablePartDefinition composable)
        {
            Exception = ex;
            ImportDefinition = import;
            ExportDefinition = export;
            ComposablePartDefinition = composable;
        }

        public override string ToString()
        {
            var msg = ComposablePartDefinition?.ToString();
            msg += Environment.NewLine;
            msg += $"--> {Exception.Message}";
            return msg;
        }
    }
}
#endif
