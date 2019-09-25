using System;

namespace Kontract.MEF.Interfaces
{
    /// <summary>
    /// Declares mandatory properties an error report for assembly loading should contain.
    /// </summary>
    public interface IErrorReport
    {
        /// <summary>
        /// Exception thrown in any process of assembly loading.
        /// </summary>
        Exception Exception { get; }
    }
}
