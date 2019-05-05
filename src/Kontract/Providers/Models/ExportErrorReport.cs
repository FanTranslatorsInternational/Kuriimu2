using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Providers.Models
{
    public class ExportErrorReport
    {
        public Exception Exception { get; }
        public ImportDefinition ImportDefinition { get; }
        public ExportDefinition ExportDefinition { get; }
        public ComposablePartDefinition ComposablePartDefinition { get; }

        public ExportErrorReport(Exception ex)
        {
            Exception = ex;
        }

        public ExportErrorReport(Exception ex, ImportDefinition import, ExportDefinition export, ComposablePartDefinition composable) : this(ex)
        {
            ImportDefinition = import;
            ExportDefinition = export;
            ComposablePartDefinition = composable;
        }
    }
}
