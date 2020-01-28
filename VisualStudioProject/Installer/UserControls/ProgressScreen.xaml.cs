using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Installer.UserControls
{
    public partial class ProgressScreen : INotifyPropertyChanged
    {
        private byte progress = 0;

        private string status = "Preparing";

        private bool startApplication = true;
        private bool uninstallApplication = false;

        public ProgressScreen()
        {
            InitializeComponent();
            DataContext = this;
        }

        //events
        private void FinishBtn_Click(object sender, RoutedEventArgs e)
        {
            if (StartApplication && !UninstallApplication)
                Process.Start(MainWindow.ApplicationFullName);

            MainWindow.ActiveWindow.Close();
        }

        //properties
        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                OnPropertyChanged("Status");
            }
        }

        public byte Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                OnPropertyChanged("Progress");

                //finish the installation
                if(progress == 100)
                {
                    //start application if update is succesfull
                    if(MainWindow.ActiveWindow.UpdateApplication)
                    {
                        Process.Start(MainWindow.ApplicationFullName);
                        this.Dispatcher.Invoke(MainWindow.ActiveWindow.Close);
                    }
                }
            }
        }

        public bool StartApplication
        {
            get { return startApplication; }
            set
            {
                startApplication = value;
                OnPropertyChanged("StartApplication");
            }
        }

        public bool UninstallApplication
        {
            get { return uninstallApplication; }
            set
            {
                uninstallApplication = value;
                OnPropertyChanged("UninstallApplication");
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
