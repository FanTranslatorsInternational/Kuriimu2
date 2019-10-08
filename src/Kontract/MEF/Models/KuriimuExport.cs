#if !NET_CORE_30
using System;
using System.ComponentModel.Composition.Primitives;

namespace Kontract.MEF.Models
{
    class KuriimuExport : Export
    {
        private readonly Action<Exception> _reportGetterException;

        public KuriimuExport(ExportDefinition exportDefinition, Func<object> exportedValueGetter, Action<Exception> reportGetterException) : base(exportDefinition, exportedValueGetter)
        {
            _reportGetterException = reportGetterException ?? throw new ArgumentNullException(nameof(reportGetterException));
        }

        protected override object GetExportedValueCore()
        {
            try
            {
                return base.GetExportedValueCore();
            }
            catch (Exception e)
            {
                ReportGetterExceptionCore(e);
                return null;
            }
        }

        protected virtual void ReportGetterExceptionCore(Exception e)
        {
            _reportGetterException(e);
        }
    }
}
#endif
