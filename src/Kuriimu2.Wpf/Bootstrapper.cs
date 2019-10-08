using System.Windows;
using Caliburn.Micro;
using Kuriimu2.ViewModels;

namespace Kuriimu2
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
