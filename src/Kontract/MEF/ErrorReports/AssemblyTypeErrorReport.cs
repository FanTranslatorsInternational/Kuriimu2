using System;
using Kontract.MEF.Interfaces;

namespace Kontract.MEF.ErrorReports
{
    /// <summary>
    /// Error report for assembly type load errors.
    /// </summary>
    public class AssemblyTypeErrorReport : IErrorReport
    {
        /// <inheritdoc cref="IErrorReport.Exception"/>
        public Exception Exception { get; }

        /// <summary>
        /// Name of the throwing adapter.
        /// </summary>
        public string AdapterName { get; }

        /// <summary>
        /// Creates a new instance of <see cref="AssemblyTypeErrorReport"/>.
        /// </summary>
        /// <param name="exc">The exception thrown while loading an assembly type.</param>
        /// <param name="adapterName">The name of the throwing adapter.</param>
        public AssemblyTypeErrorReport(Exception exc, string adapterName)
        {
            Exception = exc;
            AdapterName = adapterName;
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            var msg = AdapterName;
            msg += Environment.NewLine;
            msg += $"--> {Exception.Message}";
            return msg;
        }
    }
}
