using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace HolzTools.UserControls
{
    public partial class LedFanControl : INotifyPropertyChanged
    {
        public static readonly DependencyProperty DisplayedColorProperty =
            DependencyProperty.Register("DisplayedColor", typeof(SolidColorBrush), typeof(LedFanControl), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 0, 0)), (d, e) =>
             {
                 var s = d as LedFanControl;

                 s.displayedColor = (SolidColorBrush)e.NewValue;

                 //display the correct color if mode is static
                 if (s.Mode == "Static")
                 {
                     s.previewColor = (SolidColorBrush)e.NewValue;
                     s.OnPropertyChanged("PreviewColor");
                 }
             }));

        public static readonly DependencyProperty IsMusicModeProperty =
            DependencyProperty.Register("IsMusicMode", typeof(bool), typeof(LedFanControl), new PropertyMetadata(false, (d, e) =>
            {
                var s = d as LedFanControl;

                s.isMusicMode = (bool)e.NewValue;
            }));

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode", typeof(string), typeof(LedFanControl), new PropertyMetadata("Static", (d, e) =>
            {
                var s = d as LedFanControl;

                s.mode = (string)e.NewValue;

                if (s.mode == "Static")
                    s.PreviewColor = s.DisplayedColor;
                else if (s.mode == "Rainbow")
                    s.PreviewColor = new SolidColorBrush(Colors.Transparent);

                s.OnPropertyChanged("Mode");
            }));

        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.Register("Speed", typeof(byte), typeof(LedFanControl), new PropertyMetadata((byte)0, (d, e) =>
            {
                var s = d as LedFanControl;
                s.speed = (byte)e.NewValue;

                s.OnPropertyChanged("Speed");

                s.FanSpeed = new Duration(TimeSpan.FromMilliseconds((s.speed + 7) * 100));
            }));

        public static readonly DependencyProperty BrightnessProperty =
            DependencyProperty.Register("Brightness", typeof(byte), typeof(LedFanControl), new PropertyMetadata((byte)255, (d, e) =>
            {
                var s = d as LedFanControl;
                s.brightness = (byte)e.NewValue;

                //apply the brightness to the mode if its not rainbow
                if(s.mode != "Rainbow")
                    s.PreviewColor = new SolidColorBrush(Color.FromArgb(s.brightness, s.PreviewColor.Color.R, s.PreviewColor.Color.G, s.PreviewColor.Color.B));

                s.OnPropertyChanged("Brightness");
                s.OnPropertyChanged("OverlayColor");
            }));

        private string mode = "Static";

        private byte speed = 0;
        private byte brightness = 255;

        private bool isMusicMode = false;

        private Duration fanSpeed = new Duration(TimeSpan.FromMilliseconds(360));

        private SolidColorBrush displayedColor = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        private SolidColorBrush previewColor = new SolidColorBrush(Colors.Transparent);

        public LedFanControl()
        {
            InitializeComponent();

            new Thread(() =>
            {
                byte red = 255;
                byte green = 0;
                byte blue = 0;

                byte currentColor = 0;

                bool redGoingDown = true;
                bool greenGoingDown = false;
                bool blueGoingDown = false;

                while (true)
                {
                    //stop the animation if the mode is currently not selected or the application is minimized
                    if (((MainWindow.ActiveWindow.IsMinimized || mode != MainWindow.ActiveWindow.SelectedMode) && !isMusicMode) || (isMusicMode && MainWindow.ActiveWindow.SelectedMode != "Music"))
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    //set the correct mode arguments if it's the music preview
                    if(isMusicMode)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            switch (mode)
                            {
                                case "Static":
                                    DisplayedColor = new SolidColorBrush(Color.FromArgb(brightness, MainWindow.ActiveWindow.modeStatic.SelectedColor.R, 
                                        MainWindow.ActiveWindow.modeStatic.SelectedColor.G, MainWindow.ActiveWindow.modeStatic.SelectedColor.B));
                                    speed = 10;
                                    break;

                                case "Cycle":
                                    speed = MainWindow.ActiveWindow.modeCycle.Speed;
                                    break;

                                case "Rainbow":
                                    Speed = MainWindow.ActiveWindow.modeRainbow.Speed;
                                    break;

                                case "Color Overlay":
                                    speed = MainWindow.ActiveWindow.modeOverlay.Speed;
                                    break;
                            }
                        });
                    }

                    if (mode == "Cycle")
                    {
                        //cycle the color
                        if (redGoingDown)
                        {
                            if (red == 0)
                            {
                                redGoingDown = false;
                                greenGoingDown = true;
                            }
                            else
                            {
                                red -= 5;
                                green += 5;
                            }
                        }
                        else if (greenGoingDown)
                        {
                            if (green == 0)
                            {
                                greenGoingDown = false;
                                blueGoingDown = true;
                            }
                            else
                            {
                                green -= 5;
                                blue += 5;
                            }
                        }
                        else if (blueGoingDown)
                        {
                            if (blue == 0)
                            {
                                blueGoingDown = false;
                                redGoingDown = true;
                            }
                            else
                            {
                                blue -= 5;
                                red += 5;
                            }
                        }

                        //set the color
                        this.Dispatcher.Invoke(() => PreviewColor = new SolidColorBrush(Color.FromArgb(brightness, red, green, blue)));
                    }
                    else if (mode == "Lightning")
                    {
                        //set the fan color
                        this.Dispatcher.Invoke(() => PreviewColor = displayedColor);

                        Thread.Sleep(speed);

                        //reset the fan color after a short delay
                        this.Dispatcher.Invoke(() => PreviewColor = new SolidColorBrush(Colors.Transparent));

                        Thread.Sleep(speed * 32);
                    }
                    else if (mode == "Color Overlay")
                    {
                        currentColor++;

                        switch (currentColor)
                        {
                            case 0:
                                red = 255;
                                green = 0;
                                blue = 0;
                                break;

                            case 1:
                                red = 255;
                                green = 127;
                                blue = 0;
                                break;

                            case 2:
                                red = 255;
                                green = 255;
                                blue = 0;
                                break;

                            case 3:
                                red = 127;
                                green = 255;
                                blue = 0;
                                break;

                            case 4:
                                red = 0;
                                green = 255;
                                blue = 0;
                                break;

                            case 5:
                                red = 0;
                                green = 255;
                                blue = 127;
                                break;

                            case 6:
                                red = 0;
                                green = 255;
                                blue = 255;
                                break;

                            case 7:
                                red = 0;
                                green = 127;
                                blue = 255;
                                break;

                            case 8:
                                red = 0;
                                green = 0;
                                blue = 255;
                                break;

                            case 9:
                                red = 127;
                                green = 0;
                                blue = 255;
                                break;

                            case 10:
                                red = 255;
                                green = 0;
                                blue = 255;
                                break;

                            case 11:
                                red = 255;
                                green = 0;
                                blue = 127;
                                break;

                            case 12:
                                currentColor = 0;
                                continue;
                        }

                        this.Dispatcher.Invoke(() => PreviewColor = new SolidColorBrush(Color.FromArgb(brightness, red, green, blue)));

                        Thread.Sleep(speed * 12);
                    }

                    Thread.Sleep(speed + 1);
                }
            }).Start();
        }

        //getters and setters
        public Duration FanSpeed
        {
            get { return fanSpeed; }
            set
            {
                fanSpeed = value;
                OnPropertyChanged("FanSpeed");

                rainbowRotateStoryboard.Stop();
                rainbowRotateStoryboard.Begin();
            }
        }
        
        public bool IsMusicMode
        {
            get { return (bool)GetValue(IsMusicModeProperty); }
            set { SetValue(IsMusicModeProperty, value); }
        }
               
        public byte Speed
        {
            get { return (byte)GetValue(SpeedProperty); }
            set { SetValue(SpeedProperty, value); }
        }

        public byte Brightness
        {
            get { return (byte)GetValue(BrightnessProperty); }
            set { SetValue(BrightnessProperty, value); }
        }

        public string Mode
        {
            get { return (string)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public SolidColorBrush DisplayedColor
        {
            get { return (SolidColorBrush)GetValue(DisplayedColorProperty); }
            set { SetValue(DisplayedColorProperty, value); }
        }

        public SolidColorBrush OverlayColor
        {
            get
            {
                //return the color of the overlay that is responsible for the correct brightness to be displayed
                if (this.Background != null)
                {
                    Color backgroundColor = ((SolidColorBrush)this.Background).Color;
                    return new SolidColorBrush(Color.FromArgb((byte)(255 - brightness), backgroundColor.R, backgroundColor.G, backgroundColor.B));
                }

                return null;
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
