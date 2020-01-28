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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HolzTools.UserControls
{
    public partial class CustomColorPicker : INotifyPropertyChanged
    {
        bool madeChanges = false;

        public CustomColorPicker(Color selectedColor)
        {
            InitializeComponent();
            DataContext = this;

            colorCanvas.SelectedColor = selectedColor;
            MadeChanges = false;

            MainWindow.ActiveWindow.colorPickerBackgroundGrid.MouseUp += CancelBtn_Click;
        }

        //events
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if(MadeChanges)
            {
                AlertWindow alert = new AlertWindow("You made unsaved changes. Are you sure you want to cancel?", true);
                alert.ShowDialog();

                if(!alert.DialogResult.Value)
                {
                    return;
                }
            }

            MainWindow.ActiveWindow.ShowColorPicker = false;

            //remove the eventhandler from the colorpicker grid
            MainWindow.ActiveWindow.colorPickerBackgroundGrid.MouseUp -= CancelBtn_Click;
        }

        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ActiveWindow.ShowSettings)
            {
                MainWindow.ActiveWindow.SettingsWindow.SelectedAccentColor = (Color)colorCanvas.SelectedColor;
            }
            else
            {
                switch (MainWindow.ActiveWindow.SelectedMode)
                {
                    case "Static":
                        MainWindow.ActiveWindow.modeStatic.SelectedColor = (Color)colorCanvas.SelectedColor;
                        break;
                    case "Lightning":
                        MainWindow.ActiveWindow.modeLightning.SelectedColor = (Color)colorCanvas.SelectedColor;
                        break;
                }
            }

            MainWindow.ActiveWindow.ShowColorPicker = false;

            //remove the eventhandler from the colorpicker grid
            MainWindow.ActiveWindow.colorPickerBackgroundGrid.MouseUp -= CancelBtn_Click;
        }

        private void ColorCanvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            MadeChanges = true;
        }

        //getters and setters
        public bool MadeChanges
        {
            get { return madeChanges; }
            set
            { 
                madeChanges = value;
                OnPropertyChanged("MadeChanges");
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
