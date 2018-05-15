using System.Windows;
using Caliburn.Micro;
using Kuriimu2.ViewModels;

namespace Kuriimu2
{
    // To be configured with MEF for extensibility
    public class AppBootstrapper : BootstrapperBase
    {
        public AppBootstrapper() => Initialize();
        protected override void OnStartup(object sender, StartupEventArgs e) => DisplayRootViewFor<ShellViewModel>();
    }
}
