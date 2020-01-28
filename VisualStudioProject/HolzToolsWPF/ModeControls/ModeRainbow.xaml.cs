using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace HolzTools.ModeControls
{
    public partial class ModeRainbow : INotifyPropertyChanged
    {
        private byte speed = 0;

        private byte brightness = 255;

        private SolidColorBrush overlayColor = new SolidColorBrush(Color.FromArgb(0, 80, 80, 80));

        public ModeRainbow()
        {
            InitializeComponent();
            DataContext = this;
        }

        //getters and setters

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
