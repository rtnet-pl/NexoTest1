using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InsERT.Moria.Sfera;
using System.Windows;

namespace NexoTest1
{
    internal class AplikacjaStartowa : AplikacjaWpf
    {
        [STAThread]
        static void Main(string[] args)
        {
            AplikacjaStartowa AppStart = new AplikacjaStartowa();
            // Remove trailing space and make the app quit when main window closes:
            // If AplikacjaWpf inherits from System.Windows.Application this property exists.
            AppStart.ShutdownMode = ShutdownMode.OnMainWindowClose;
            AppStart.StartupUri = new System.Uri("MainWindow.xaml", System.UriKind.Relative);
            AppStart.Uruchom();
        }
    }
}
