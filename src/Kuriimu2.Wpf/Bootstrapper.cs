using System.Windows;
using Caliburn.Micro;
using Kuriimu2.Wpf.ViewModels;

namespace Kuriimu2.Wpf
{
    public class AppBootstrapper : BootstrapperBase
    {
        public AppBootstrapper() => Initialize();

        public static string[] Args;

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            Args = e.Args;
            DisplayRootViewFor<ShellViewModel>();
        }
    }
}
