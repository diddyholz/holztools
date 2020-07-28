using HolzTools.UserControls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HolzTools.ModeControls
{
    public partial class ModeStatic : INotifyPropertyChanged
    {
        private Color selectedColor;
        private SolidColorBrush previewColor;

        private Color realColor;

        private byte brightness = 255;
        private byte type = 0; // 0 for single; 1 for multi

        private bool isDefault = false;

        public ModeStatic()
        {
            InitializeComponent();
            DataContext = this;
            SelectedColor = Color.FromRgb(255, 0, 0);
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
        private void DefaultColorButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            SolidColorBrush color = (SolidColorBrush)btn.Background;
            SelectedColor = color.Color;
        }

        private void CustomColorBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.colorPickerViewBox.Child = new CustomColorPicker(SelectedColor, CustomColorPicker.ColorToBeChanged.StaticSelectedColor);
            MainWindow.ActiveWindow.ShowColorPicker = true;
        }

        private void ShowEditor_Click(object sender, RoutedEventArgs e)
        {
            // trim or expand the led list if needed
            List<Color> ledList = MainWindow.ActiveWindow.SelectedLedItem.LedColorList;

            if (ledList.Count < MainWindow.ActiveWindow.SelectedLedItem.LedCount)
                while (ledList.Count < MainWindow.ActiveWindow.SelectedLedItem.LedCount)
                    ledList.Add(Color.FromRgb(200, 0, 0));
            else if (ledList.Count > MainWindow.ActiveWindow.SelectedLedItem.LedCount)
                ledList.RemoveRange(MainWindow.ActiveWindow.SelectedLedItem.LedCount, ledList.Count - MainWindow.ActiveWindow.SelectedLedItem.LedCount);

            MainWindow.ActiveWindow.multiColorEditorViewbox.Child = new MultiColorEditor(ledList);
            MainWindow.ActiveWindow.ShowMultiColorEditorWindow = true;
        }

        //getters and settters
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
                PreviewColor = new SolidColorBrush(Color.FromArgb(brightness, selectedColor.R, selectedColor.G, selectedColor.B));
                RealColor = Color.FromRgb((byte)((float)selectedColor.R * ((float)brightness / 255.00)), (byte)((float)selectedColor.G * ((float)brightness / 255.00)), (byte)((float)selectedColor.B * ((float)brightness / 255.00)));
                OnPropertyChanged("SelectedColor");

                MainWindow.ActiveWindow.MadeChanges = true;
            }
        }

        public SolidColorBrush PreviewColor     //the color used for the preview, alpha channel is the brightness
        {
            get { return previewColor; }
            set
            {
                previewColor = value;
                OnPropertyChanged("PreviewColor");
            }
        }

        public Color RealColor                  //the color used to send usb data, does not use alpha channel for brightness, brightness is directly multiplied to colors (color * brightness)
        {
            get { return realColor; }
            set
            {
                realColor = value;
                OnPropertyChanged("RealColor");
            }
        }

        public byte Brightness
        {
            get { return brightness; }
            set
            {
                brightness = value;
                PreviewColor = new SolidColorBrush(Color.FromArgb(brightness, selectedColor.R, selectedColor.G, selectedColor.B));
                RealColor = Color.FromRgb((byte)((float)selectedColor.R * ((float)brightness / 255.00)), (byte)((float)selectedColor.G * ((float)brightness / 255.00)), (byte)((float)selectedColor.B * ((float)brightness / 255.00)));
                OnPropertyChanged("Brightness");

                MainWindow.ActiveWindow.MadeChanges = true;
            }
        }

        public byte Type
        {
            get { return type; }
            set
            {
                type = value;
                OnPropertyChanged("Type");
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
