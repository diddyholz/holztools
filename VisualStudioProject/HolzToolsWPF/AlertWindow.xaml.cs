using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HolzTools
{
    public partial class AlertWindow : INotifyPropertyChanged
    {
        private string message = "";
        private bool value = false;
        private bool useYesNoBtns = false;

        public AlertWindow(string message, bool useYesNoBtns = false)
        {
            InitializeComponent();
            DataContext = this;
            Message = message;

            if(useYesNoBtns)
            {
                YNBtnGrid.Visibility = Visibility.Visible;
                OKBtnGrid.Visibility = Visibility.Collapsed;
                this.useYesNoBtns = true;
            }
        }

        //events
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ActiveWindow.BlockPopups && !useYesNoBtns)
                this.Close();
            else
                System.Media.SystemSounds.Asterisk.Play();

            Window window = (Window)sender;
            window.Topmost = true;
            window.Activate();
        }

        private void YesNoBtn(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;

            if (btn.Name == "yesBtn")
                value = true;
            else
                value = false;

            DoubleAnimation closeAnim = new DoubleAnimation()
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(100))
            };

            closeAnim.Completed += ClosingStoryboard_Completed;
            this.BeginAnimation(Window.OpacityProperty, closeAnim);
        }
        
        private void ClosingStoryboard_Completed(object sender, EventArgs e)
        {
            if(this.DialogResult == null)
                this.DialogResult = value;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }

        //getters and setters
        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                OnPropertyChanged("Message");
            }
        } 

        public Color AccentColor
        {
            get { return MainWindow.ActiveWindow.AccentColor; }
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
