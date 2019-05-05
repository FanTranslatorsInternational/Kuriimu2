using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Kontract.Providers.Models;

namespace Kontract.Providers
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

                composablePartDefinition.CreatePart().Activate();
                yield return new Export(exportDefinition, () => GetExportedValue(composablePartDefinition, exportDefinition));
            }
        }

        private IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetCatalogExports(ImportDefinition definition)
        {
            var parts = _catalog.Parts;
            foreach (var part in parts)
                foreach (var exportDefinition in part.ExportDefinitions)
                {
                    bool result = false;
                    try
                    {
                        result = definition.IsConstraintSatisfiedBy(exportDefinition);
                    }
                    catch (Exception e)
                    {
                        ErrorReports.Add(new ExportErrorReport(e, definition, exportDefinition, part));
                    }

                    var retValue = new Tuple<ComposablePartDefinition, ExportDefinition>(part, exportDefinition);
                    yield return result ? retValue : new Tuple<ComposablePartDefinition, ExportDefinition>(null, null);
                }
        }

        private object GetExportedValue(ComposablePartDefinition composablePartDefinition, ExportDefinition exportDefinition)
        {
            return composablePartDefinition.CreatePart().GetExportedValue(exportDefinition);
        }
    }
}
