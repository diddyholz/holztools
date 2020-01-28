using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Media;

namespace HolzTools.ModeControls
{
    public partial class ModeCycle : INotifyPropertyChanged
    {
        private byte brightness = 255;
        private byte speed = 0;

        private SolidColorBrush previewColor = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));

        public ModeCycle()
        {
            InitializeComponent();
            DataContext = this;
            Thread cycleThread = new Thread(new ThreadStart(colorCycle));
            cycleThread.IsBackground = true;
            cycleThread.Start();
        }

        private void colorCycle()
        {
            bool redGoingDown = true;
            bool greenGoingDown = false;
            bool blueGoindDown = false;

            byte red = 255;
            byte green = 0;
            byte blue = 0;

            while (true)
            {
                if (MainWindow.ActiveWindow.IsMinimized)
                {
                    Thread.Sleep(100);
                    break;
                }

                if (redGoingDown)
                {
                    if (red == 0)
                    {
                        redGoingDown = false;
                        greenGoingDown = true;
                    }
                    else
                    {
                        red--;
                        green++;
                    }
                }
                else if (greenGoingDown)
                {
                    if (green == 0)
                    {
                        greenGoingDown = false;
                        blueGoindDown = true;
                    }
                    else
                    {
                        green--;
                        blue++;
                    }
                }
                else if (blueGoindDown)
                {
                    if (blue == 0)
                    {
                        blueGoindDown = false;
                        redGoingDown = true;
                    }
                    else
                    {
                        blue--;
                        red++;
                    }
                }

                try
                {
                    this.Dispatcher.BeginInvoke(new Action(() => { PreviewColor = new SolidColorBrush(Color.FromArgb(brightness, red, green, blue)); }));
                }
                catch { }

                Thread.Sleep(speed + 1);
            }
        }

        //getter and setter
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

        public SolidColorBrush PreviewColor
        {
            get { return previewColor; }
            set
            {
                previewColor = value;
                OnPropertyChanged("PreviewColor");
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
