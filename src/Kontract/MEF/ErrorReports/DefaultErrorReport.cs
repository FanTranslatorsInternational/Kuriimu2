using System;
using Kontract.MEF.Interfaces;

namespace Kontract.MEF.ErrorReports
{
    /// <summary>
    /// Default implementation of <see cref="IErrorReport"/>.
    /// </summary>
    class DefaultErrorReport : IErrorReport
    {
        /// <inheritdoc cref="IErrorReport.Exception"/>
        public Exception Exception { get; }

        /// <summary>
        /// Creates new instance of <see cref="DefaultErrorReport"/>.
        /// </summary>
        /// <param name="exc">Exception thrown on any process.</param>
        public DefaultErrorReport(Exception exc)
        {
            Exception = exc;
        }

        public override string ToString()
        {
            return Exception.Message;
        }
    }
}
