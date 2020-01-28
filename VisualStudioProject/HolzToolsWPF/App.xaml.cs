using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace HolzTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string[] Args;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //kill the existing window
            foreach(Process p in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
            {
                if(p.Id != Process.GetCurrentProcess().Id)
                    p.Kill();
            }

            if (e.Args.Length > 0)
            {
                Args = e.Args;
            }
        }

        [STAThread]
        public static void Main()
        {
            var application = new App();
            application.InitializeComponent();
            application.Run();
        }
    }
}
