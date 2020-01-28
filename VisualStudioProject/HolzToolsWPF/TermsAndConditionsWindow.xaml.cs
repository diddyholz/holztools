using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Animation;

namespace HolzTools
{
    public partial class TermsAndConditionsWindow : Window
    {
        public TermsAndConditionsWindow()
        {
            InitializeComponent();

            //load all licenses
            mitLicenseTextBlock.Text = Properties.Resources.MIT_txt;

            apacheLicenseTextBlock.Text = Properties.Resources.Apache_2_0_txt;

            cpolLicenseTextBlock.Text = Properties.Resources.CPOL_1_02_txt;

            msplLicenseTextBlock.Text = Properties.Resources.MS_PL_txt;
        }

        //events
        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
