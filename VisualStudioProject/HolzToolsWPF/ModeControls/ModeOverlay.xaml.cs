using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HolzTools.ModeControls
{
    public partial class ModeOverlay : INotifyPropertyChanged
    {
        private SolidColorBrush fanColor = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));

        private byte speed = 0;
        private byte brightness = 255;
        private byte direction = 1;

        public ModeOverlay()
        {
            InitializeComponent();
            DataContext = this;
        }

        //events
        private void DirBtn_Click(object sender, RoutedEventArgs e)
        {
            Button pressedButton = (Button)sender;

            if (pressedButton.Name == "dirLBtn")
            {
                Direction = 0;
            }
            else
            {
                Direction = 1;
            }
        }

        //getters and setters
        public SolidColorBrush FanColor
        {
            get { return fanColor; }
            set
            {
                fanColor = value;
                OnPropertyChanged("FanColor");
            }
        }

        public byte Brightness
        {
            get { return brightness; }
            set
            {
                brightness = value;
                OnPropertyChanged("Brightness");

                MainWindow.ActiveWindow.MadeChanges = true;
            }
        }

        public byte Direction
        {
            get { return direction; }
            set
            {
                direction = value;
                OnPropertyChanged("Direction");

                if (Direction == 0)
                {
                    dirRBtn.Foreground = new SolidColorBrush(Colors.White);
                    dirLBtn.SetResourceReference(Control.ForegroundProperty, "AccentColor");
                }
                else
                {
                    dirLBtn.Foreground = new SolidColorBrush(Colors.White);
                    dirRBtn.SetResourceReference(Control.ForegroundProperty, "AccentColor");
                }

                MainWindow.ActiveWindow.MadeChanges = true;
            }
        }

        public byte Speed
        {
            get { return speed; }
            set
            {
                speed = value;
                OnPropertyChanged("Speed");

                MainWindow.ActiveWindow.MadeChanges = true;
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
