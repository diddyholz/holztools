using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for ConfirmUninstallScreen.xaml
    /// </summary>
    public partial class ConfirmUninstallScreen : UserControl
    {
        public ConfirmUninstallScreen()
        {
            InitializeComponent();
        }

        //events
        private void UninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.SetInstallerState(MainWindow.InstallerState.Uninstalling);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.Close();
        }
    }
}
