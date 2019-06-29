#if !NET_CORE_21
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kontract.MEF.ErrorReports;
using Kontract.MEF.Interfaces;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

// Source: https://stackoverflow.com/questions/4144683/handle-reflectiontypeloadexception-during-mef-composition
namespace Kontract.MEF.Catalogs
{
    /// <summary>
    /// Partial reimplementation of <see cref="DirectoryCatalog"/> to catch bad assembly types.
    /// </summary>
    class KuriimuDirectoryCatalog : ComposablePartCatalog
    {
        private readonly AggregateCatalog _catalog;

        /// <summary>
        /// Contains all assembly type errors.
        /// </summary>
        public IList<IErrorReport> ErrorReports { get; }

        /// <summary>
        /// Creates a new instance of <see cref="KuriimuDirectoryCatalog"/>.
        /// </summary>
        /// <param name="directory">The directory to load assemblies from.</param>
        /// <param name="searchPattern">The pattern to filter found assemblies through.</param>
        public KuriimuDirectoryCatalog(string directory, string searchPattern)
        {
            _catalog = new AggregateCatalog();
            ErrorReports = new List<IErrorReport>();

            var files = Directory.EnumerateFiles(directory, searchPattern, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var asmCat = new AssemblyCatalog(file);

                    //Force MEF to load the plugin and figure out if there are any exports
                    // good assemblies will not throw the RTLE exception and can be added to the catalog
                    if (asmCat.Parts.ToList().Count > 0)
                        _catalog.Catalogs.Add(asmCat);
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    foreach (var loaderExc in rtle.LoaderExceptions)
                        if (loaderExc is TypeLoadException tle)
                            ErrorReports.Add(new AssemblyTypeErrorReport(loaderExc, tle.TypeName));
                        else
                            ErrorReports.Add(new DefaultErrorReport(loaderExc));
                }
                catch (BadImageFormatException)
                {
                }
            }
        }

        public override IQueryable<ComposablePartDefinition> Parts => _catalog.Parts;
    }
}
#endif
