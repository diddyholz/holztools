using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Installer.UserControls
{
    public partial class InstallOptionsScreen : INotifyPropertyChanged
    {
        private bool createDesktopShortcut = true;
        private bool createStartMenuShortcut = true;
        private bool autoStart = true;

        private string installationDirectory = "";

        public InstallOptionsScreen()
        {
            InitializeComponent();
            DataContext = this; 
            InstallationDirectory = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        }

        //events
        private void OpenDirectoryDialog_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog() { Description = "Choose the installation directory" })
            {
                DialogResult result = dialog.ShowDialog();

                if(result == DialogResult.OK)
                {
                    InstallationDirectory = dialog.SelectedPath;
                }
            }
        }

        private void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.SetInstallerState(MainWindow.InstallerState.Installing);
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            new TermsAndConditionsWindow().ShowDialog();
        }

        //getters and setters
        public bool CreateDesktopShortcut
        {
            get { return createDesktopShortcut; }
            set
            {
                createDesktopShortcut = value;
                OnPropertyChanged("CreateDesktopShortcut");
            }
        }

        public bool CreateStartMenuShortcut
        {
            get { return createStartMenuShortcut; }
            set
            {
                createStartMenuShortcut = value;
                OnPropertyChanged("CreateStartMenuShortcut");
            }
        }

        public bool AutoStart
        {
            get { return autoStart; }
            set
            {
                autoStart = value;
                OnPropertyChanged("AutoStart");
            }
        }

        public string InstallationDirectory
        {
            get { return installationDirectory; }
            set
            {
                installationDirectory = value;

                if (!value.Contains(MainWindow.ProgramName))
                    installationDirectory += $@"\{MainWindow.ProgramName}\";
                
                OnPropertyChanged("InstallationDirectory");
            }
        }

        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
