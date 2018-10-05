using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GCL.WPF_App
{
    public class Application : System.Windows.Application
    {
        public Application()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(default_UnhandledExceptionEventHandler);
        }
        static void default_UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var exception = args.ExceptionObject as Exception;
            MessageBox.Show(
                String.Format("[{0}]\n{1}", args.ExceptionObject.GetType(), exception.Message),
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        public new void Shutdown()
        {
            base.Shutdown();
        }
    }
}
