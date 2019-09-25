#if !NET_CORE_21
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Kontract.MEF.ErrorReports;
using Kontract.MEF.Interfaces;
using Kontract.Providers.Models;

namespace Kontract.MEF.Providers
{
    internal class KuriimuExportProvider : ExportProvider
    {
        private readonly ComposablePartCatalog _catalog;

        public bool HasErrorReports => ErrorReports.Any();
        public IList<ExportErrorReport> ErrorReports { get; }

        public KuriimuExportProvider(ComposablePartCatalog catalog)
        {
            _catalog = catalog;
            ErrorReports = new List<ExportErrorReport>();
        }

        protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition)
        {
            // Called once per Import definition of the given parent object
            var exports = GetCatalogExports(definition);
            foreach (var (composablePartDefinition, exportDefinition) in exports)
            {
                if (composablePartDefinition == null || exportDefinition == null)
                    continue;

                yield return new KuriimuExport(exportDefinition,
                    () => GetExportedValue(composablePartDefinition, exportDefinition),
                    ex => ReportError(ex, definition, exportDefinition, composablePartDefinition));
            }
        }

        private IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetCatalogExports(ImportDefinition definition)
        {
            foreach (var composablePartDefinition in _catalog.Parts)
            {
                foreach (var exportDefinition in composablePartDefinition.ExportDefinitions)
                {
                    var result = false;
                    try
                    {
                        result = definition.IsConstraintSatisfiedBy(exportDefinition);
                    }
                    catch (Exception e)
                    {
                        ReportError(e, definition, exportDefinition, composablePartDefinition);
                    }

                    var retValue = new Tuple<ComposablePartDefinition, ExportDefinition>(composablePartDefinition, exportDefinition);
                    yield return result ? retValue : new Tuple<ComposablePartDefinition, ExportDefinition>(null, null);
                }
            }
        }

        private object GetExportedValue(ComposablePartDefinition composablePartDefinition, ExportDefinition exportDefinition)
        {
            return composablePartDefinition.CreatePart().GetExportedValue(exportDefinition);
        }

        private void ReportError(Exception e, ImportDefinition definition, ExportDefinition exportDefinition, ComposablePartDefinition part)
        {
            // Check if error already existing (somehow this export provider gets called multiple times)
            if (!ErrorReports.Any(er => er.ImportDefinition == definition && er.ExportDefinition == exportDefinition))
                ErrorReports.Add(new ExportErrorReport(e, definition, exportDefinition, part));
        }
    }
}
#endif
