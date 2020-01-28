using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace HolzTools.UserControls
{
    public partial class LedStripControl : INotifyPropertyChanged
    { 
        public static readonly DependencyProperty DisplayedColorProperty =
            DependencyProperty.Register("DisplayedColor", typeof(SolidColorBrush), typeof(LedStripControl), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255,0,0)), (d, e) => 
            {
                var s = d as LedStripControl;

                s.displayedRed = ((SolidColorBrush)e.NewValue).Color.R;
                s.displayedGreen = ((SolidColorBrush)e.NewValue).Color.G;
                s.displayedBlue = ((SolidColorBrush)e.NewValue).Color.B;

                //set the color of all leds to the displayed Color
                foreach (LED led in s.LedList)
                {
                    led.Color = (SolidColorBrush)e.NewValue;
                }

                s.OnPropertyChanged("LedList");
            }));

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode", typeof(string), typeof(LedStripControl), new PropertyMetadata("Static", (d, e) => 
            {
                var s = d as LedStripControl;

                //reinitilize the leds 
                foreach(LED led in s.LedList)
                {
                    led.RainbowIsInit = false;
                }

                s.OnPropertyChanged("LedList");

                s.mode = (string)e.NewValue;
            }));
        
        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.Register("Speed", typeof(byte), typeof(LedStripControl), new PropertyMetadata((byte)0, (d, e) =>
            {
                var s = d as LedStripControl;

                s.speed = (byte)e.NewValue;
            }));
        
        public static readonly DependencyProperty BrightnessProperty =
            DependencyProperty.Register("Brightness", typeof(byte), typeof(LedStripControl), new PropertyMetadata((byte)255, (d, e) =>
            {
                var s = d as LedStripControl;

                if (s.mode != "Rainbow" && s.mode != "Cycle")
                {
                    foreach (LED led in s.LedList)
                    {
                        led.Color = new SolidColorBrush(Color.FromArgb(s.brightness, led.Color.Color.R, led.Color.Color.G, led.Color.Color.B));
                    }
                }

                s.OnPropertyChanged("LedList");

                s.brightness = (byte)e.NewValue;
            }));

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register("Direction", typeof(byte), typeof(LedStripControl), new PropertyMetadata((byte)0, (d, e) =>
            {
                var s = d as LedStripControl;

                s.direction = (byte)e.NewValue;
            }));

        public static readonly DependencyProperty IsMusicModeProperty =
            DependencyProperty.Register("IsMusicMode", typeof(bool), typeof(LedStripControl), new PropertyMetadata(false, (d, e) =>
            {
                var s = d as LedStripControl;

                s.isMusicMode = (bool)e.NewValue;
            }));

        public List<LED> ledList = new List<LED>();

        private string mode = "Static";

        private byte direction = 0;
        private byte speed = 0;
        private byte brightness = 255;

        private byte displayedRed = 255;
        private byte displayedGreen = 0;
        private byte displayedBlue = 0;

        private bool isMusicMode = false;

        public LedStripControl()
        {
            //init the LED Colors
            for(byte x = 0; x < 13; x++)
            {
                LED led = new LED(x);
                led.Color = DisplayedColor;
                LedList.Add(led);
            }

            InitializeComponent();

            //thread that updates the LEDs
            new Thread(() =>
            {
                int currentLed = 0;

                byte currentColor = 0;
                byte red = 0;
                byte green = 0;
                byte blue = 0;

                while (true)
                {
                    if (((MainWindow.ActiveWindow.IsMinimized || mode != MainWindow.ActiveWindow.SelectedMode) && !isMusicMode) || (isMusicMode && MainWindow.ActiveWindow.SelectedMode != "Music"))
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    //set the correct mode arguments if it's the music preview
                    if (isMusicMode)
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

                    if (mode == "Rainbow")
                    {
                        foreach (LED led in LedList)
                        {
                            if (!led.RainbowIsInit)
                                led.InitRainbow();

                            led.CycleColor(brightness);
                        }
                    }
                    else if (mode == "Cycle")
                    {
                        //get the color from the first led and apply it to all others
                        if (!LedList[0].RainbowIsInit)
                            LedList[0].InitRainbow();

                        LedList[0].CycleColor(brightness);

                        for(int x = 1; x < LedList.Count; x++)
                        {
                            LedList[x].Color = LedList[0].Color;
                        }
                    }
                    else if (mode == "Color Overlay")
                    {
                        if (currentLed == LedList.Count || currentLed == -1)
                        {
                            if (direction == 0)
                            {
                                currentLed = LedList.Count - 1;
                            }
                            else
                            {
                                currentLed = 0;
                            }

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
                        }

                        LedList[currentLed].Color = new SolidColorBrush(Color.FromArgb(brightness, red, green, blue));

                        if (direction == 0)
                        {
                            currentLed--;
                        }
                        else
                        {
                            currentLed++;
                        }
                    }
                    else if (mode == "Lightning")
                    {
                        if (currentLed == LedList.Count + 20 || currentLed == -20)
                        {
                            if (direction == 0)
                            {
                                currentLed = LedList.Count - 1;
                            }
                            else
                            {
                                currentLed = 0;
                            }

                        }

                        for (int x = 0; x < LedList.Count; x++)
                        {
                            if (x == currentLed)
                                LedList[x].Color = new SolidColorBrush(Color.FromRgb(displayedRed, displayedGreen, displayedBlue));
                            else
                                LedList[x].Color = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                        }

                        if (direction == 0)
                        {
                            currentLed--;
                        }
                        else
                        {
                            currentLed++;
                        }
                    }

                    OnPropertyChanged("LedList");

                    Thread.Sleep(speed + 1);
                }
            })
            { IsBackground = true }.Start();
        }

        //Properties
        public List<LED> LedList
        {
            get { return ledList; }
        }

        public SolidColorBrush DisplayedColor
        {
            get { return (SolidColorBrush)GetValue(DisplayedColorProperty); }
            set { SetValue(DisplayedColorProperty, value); }
        }

        public string Mode
        {
            get { return (string)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public byte Speed
        {
            get { return (byte)GetValue(SpeedProperty); }
            set { SetValue(SpeedProperty, value); }
        }

        public bool IsMusicMode
        {
            get { return (bool)GetValue(IsMusicModeProperty); }
            set { SetValue(IsMusicModeProperty, value); }
        }

        public byte Brightness
        {
            get { return (byte)GetValue(BrightnessProperty); }
            set { SetValue(BrightnessProperty, value); }
        }

        public byte Direction
        {
            get { return (byte)GetValue(DirectionProperty); }
            set { SetValue(DirectionProperty, value); }
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

    public class LED
    {
        private bool redGoingDown = true;
        private bool greenGoingDown = false;
        private bool blueGoingDown = false;
        private bool rainbowIsInit = false;

        private byte red = 0;
        private byte green = 0;
        private byte blue = 0;
        private byte index = 0;

        private Color color;

        public LED(byte index)
        {
            this.index = index;
        }

        public void CycleColor(byte brightness)
        {
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

            color = System.Windows.Media.Color.FromArgb(brightness, red, green, blue);
        }

        public void InitRainbow()
        {
            switch (index)
            {
                case 0:
                    red = 255;
                    green = 0;
                    blue = 0;
                    redGoingDown = true;
                    greenGoingDown = false;
                    blueGoingDown = false;
                    break;

                case 1:
                    red = 195;
                    green = 60;
                    blue = 0;
                    redGoingDown = true;
                    greenGoingDown = false;
                    blueGoingDown = false;
                    break;

                case 2:
                    red = 130;
                    green = 125;
                    blue = 0;
                    redGoingDown = true;
                    greenGoingDown = false;
                    blueGoingDown = false;
                    break;

                case 3:
                    red = 65;
                    green = 190;
                    blue = 0;
                    redGoingDown = true;
                    greenGoingDown = false;
                    blueGoingDown = false;
                    break;

                case 4:
                    red = 0;
                    green = 255;
                    blue = 0;
                    redGoingDown = false;
                    greenGoingDown = true;
                    blueGoingDown = false;
                    break;

                case 5:
                    red = 0;
                    green = 195;
                    blue = 60;
                    redGoingDown = false;
                    greenGoingDown = true;
                    blueGoingDown = false;
                    break;

                case 6:
                    red = 0;
                    green = 130;
                    blue = 125;
                    redGoingDown = false;
                    greenGoingDown = true;
                    blueGoingDown = false;
                    break;

                case 7:
                    red = 0;
                    green = 65;
                    blue = 190;
                    redGoingDown = false;
                    greenGoingDown = true;
                    blueGoingDown = false;
                    break;

                case 8:
                    red = 0;
                    green = 0;
                    blue = 255;
                    redGoingDown = false;
                    greenGoingDown = false;
                    blueGoingDown = true;
                    break;

                case 9:
                    red = 60;
                    green = 0;
                    blue = 195;
                    redGoingDown = false;
                    greenGoingDown = false;
                    blueGoingDown = true;
                    break;

                case 10:
                    red = 125;
                    green = 0;
                    blue = 130;
                    redGoingDown = false;
                    greenGoingDown = false;
                    blueGoingDown = true;
                    break;

                case 11:
                    red = 190;
                    green = 0;
                    blue = 65;
                    redGoingDown = false;
                    greenGoingDown = false;
                    blueGoingDown = true;
                    break;

                case 12:
                    red = 255;
                    green = 0;
                    blue = 0;
                    redGoingDown = true;
                    greenGoingDown = false;
                    blueGoingDown = false;
                    break;
            }

            RainbowIsInit = true;
        }

        public bool RainbowIsInit
        {
            get; set;
        }

        public SolidColorBrush Color
        {
            get { return new SolidColorBrush(color); }
            set 
            {
                RainbowIsInit = false;
                
                if(value != null)
                    color = value.Color; 
            }
        }
    }
}
