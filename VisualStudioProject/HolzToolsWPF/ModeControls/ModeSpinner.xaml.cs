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
    public partial class ModeSpinner : INotifyPropertyChanged
    {
        private bool isDefault = false;

        private byte speed;
        private byte length;

        private Color spinnerColor = Color.FromArgb(255, 255, 0, 0);
        private Color backgroundColor = Color.FromArgb(255, 255, 255, 255);

        public ModeSpinner()
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
            SpinnerColor = SpinnerColor;
            BackgroundColor = BackgroundColor;
        }

        private void DefaultSpinnerColorButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            SolidColorBrush temp = (SolidColorBrush)btn.Background;
            SpinnerColor = temp.Color;
        }

        private void DefaultBackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            SolidColorBrush temp = (SolidColorBrush)btn.Background;
            BackgroundColor = temp.Color;
        }

        private void CustomSpinnerColorBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.colorPickerViewBox.Child = new CustomColorPicker(SpinnerColor);
            MainWindow.ActiveWindow.ShowColorPicker = true;
        }

        private void CustomBackgroundColorBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.colorPickerViewBox.Child = new CustomColorPicker(SpinnerColor);
            MainWindow.ActiveWindow.ShowColorPicker = true;
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

        public byte Length
        {
            get { return length; }
            set
            {
                length = value;
                OnPropertyChanged("Length");

                MainWindow.ActiveWindow.MadeChanges = true;
            }
        }

        public Color SpinnerColor
        {
            get { return spinnerColor; }
            set
            {
                isDefault = false;

                customSpinnerColorBtn.Tag = "NotSelected";

                //select the new button
                foreach (Button defaultColorBtn in FindVisualChildren<Button>(spinnerColorBtnGrid))
                {
                    SolidColorBrush background = (SolidColorBrush)defaultColorBtn.Background;

                    if (background.Color == SpinnerColor && background.Color != value)
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
                    customSpinnerColorBtn.Tag = "Selected";
                }

                spinnerColor = value;
                OnPropertyChanged("SelectedColor");

                MainWindow.ActiveWindow.MadeChanges = true;
            }
        }

        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set
            {
                isDefault = false;

                customBackgroundColorBtn.Tag = "NotSelected";

                //select the new button
                foreach (Button defaultColorBtn in FindVisualChildren<Button>(backgroundColorBtnGrid))
                {
                    SolidColorBrush background = (SolidColorBrush)defaultColorBtn.Background;

                    if (background.Color == BackgroundColor && background.Color != value)
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
                    customBackgroundColorBtn.Tag = "Selected";
                }

                backgroundColor = value;
                OnPropertyChanged("SelectedColor");

                MainWindow.ActiveWindow.MadeChanges = true;
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
