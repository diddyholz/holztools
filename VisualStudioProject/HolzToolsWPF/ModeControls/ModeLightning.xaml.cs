using HolzTools.UserControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HolzTools.ModeControls
{
    public partial class ModeLightning : INotifyPropertyChanged
    {
        private byte brightness = 255;

        private bool isDefault = false;

        private Color selectedColor = Color.FromArgb(255, 255, 0, 0);
        private SolidColorBrush realColor = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        private SolidColorBrush fanColor = new SolidColorBrush(Color.FromRgb(255,0,0));

        public ModeLightning()
        {
            InitializeComponent();
            DataContext = this;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        //events
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //reset the selected Color because else the default color wouldnt get selected
            SelectedColor = SelectedColor;
        }

        private void DefaultColorButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            SolidColorBrush temp = (SolidColorBrush)btn.Background;
            SelectedColor = temp.Color;
        }

        private void CustomColorBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.colorPickerViewBox.Child = new CustomColorPicker(SelectedColor, CustomColorPicker.ColorToBeChanged.LightningSelectedColor);
            MainWindow.ActiveWindow.ShowColorPicker = true;
        }

        //getters and setters
        public byte Brightness
        {
            get { return brightness; }
            set
            {
                brightness = value;
                RealColor = new SolidColorBrush(Color.FromRgb((byte)((float)selectedColor.R * ((float)brightness / 255.00)), (byte)((float)selectedColor.G * (float)((float)brightness / 255.00)), (byte)((float)selectedColor.B * (float)((float)brightness / 255.00))));
                OnPropertyChanged("Brightness");

                MainWindow.ActiveWindow.MadeChanges = true;
            }
        }

        public SolidColorBrush FanColor
        {
            get { return fanColor; }
            set
            {
                fanColor = value;
                OnPropertyChanged("FanColor");
            }
        }

        public Color SelectedColor
        {
            get { return selectedColor; }
            set
            {
                isDefault = false;

                customColorBtn.Tag = "NotSelected";

                //select the new button
                foreach (Button defaultColorBtn in FindVisualChildren<Button>(this))
                {
                    SolidColorBrush background = (SolidColorBrush)defaultColorBtn.Background;

                    if (background.Color == SelectedColor && background.Color != value)
                    {
                        defaultColorBtn.Tag = "NotSelected";
                    }
                    else if (background.Color == value)
                    {
                        defaultColorBtn.Tag = "Selected";
                        isDefault = true;
                    }
                }

                if (!isDefault)
                {
                    customColorBtn.Tag = "Selected";
                }

                selectedColor = value;
                RealColor = new SolidColorBrush(Color.FromRgb((byte)((float)selectedColor.R * ((float)brightness / 255.00)), (byte)((float)selectedColor.G * (float)((float)brightness / 255.00)), (byte)((float)selectedColor.B * (float)((float)brightness / 255.00))));
                OnPropertyChanged("SelectedColor");

                MainWindow.ActiveWindow.MadeChanges = true;
            }
        }

        public SolidColorBrush RealColor
        {
            get { return realColor; }
            set
            {
                realColor = value;
                OnPropertyChanged("RealColor");
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
