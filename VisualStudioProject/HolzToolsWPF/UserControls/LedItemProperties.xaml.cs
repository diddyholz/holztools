using System.ComponentModel;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HolzTools.UserControls
{
    public partial class LedItemProperties : INotifyPropertyChanged
    {
        string[] availablePorts = null;

        private string selectedComPort = "COM";
        private string selectedItemName = "LED";

        private int selectedBaud = 0;
        private int selectedType = 0;  //0 for ARGB, 1 for 4Pin RGB
        private int selectedLedCount = 0;
        private int selectedDPin = 0;
        private int selectedRPin = 0;
        private int selectedGPin = 0;
        private int selectedBPin = 0;

        private bool madeChanges = false;

        private static readonly Regex _regex = new Regex("[^0-9]+"); 

        private LedItem ledItem;

        public LedItemProperties(string name, int type, string comPort, int ledCount, int dPin, int rPin, int gPin, int bPin, int bautRate, LedItem ledItem)
        {
            InitializeComponent();
            DataContext = this;
            RefreshPorts();
            comPortComboBox.ItemsSource = AvailablePorts;

            this.ledItem = ledItem;

            SelectedItemName = name;
            SelectedType = type;
            SelectedComPort = comPort;
            SelectedLedCount = ledCount;
            SelectedDPin = dPin;
            SelectedRPin = rPin;
            SelectedGPin = gPin;
            SelectedBPin = bPin;
            SelectedBaud = bautRate;

            MainWindow.ActiveWindow.propertiesBackgroundGrid.MouseUp += CancelBtn_Click;
        }

        public void RefreshPorts()
        {
            AvailablePorts = SerialPort.GetPortNames();
        }

        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        //events
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MadeChanges)
            {
                AlertWindow alert = new AlertWindow("You made unsaved changes. Are you sure you want to cancel?", true);
                alert.Owner = MainWindow.ActiveWindow;
                alert.ShowDialog();

                if (alert.DialogResult.Value)
                {
                    //remove the properties window
                    MainWindow.ActiveWindow.ShowProperties = false;

                    //remove the eventhandler from the properties grid
                    MainWindow.ActiveWindow.propertiesBackgroundGrid.MouseUp -= CancelBtn_Click;
                }
            }
            else
            {
                MainWindow.ActiveWindow.ShowProperties = false;

                //remove the eventhandler from the properties grid
                MainWindow.ActiveWindow.propertiesBackgroundGrid.MouseUp -= CancelBtn_Click;
            }
        }

        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            //save the settings
            ledItem.ItemName = SelectedItemName;
            ledItem.Type = SelectedType;
            ledItem.ComPortName = SelectedComPort;
            ledItem.LedCount = SelectedLedCount;
            ledItem.DPIN = SelectedDPin;
            ledItem.RPIN = SelectedRPin;
            ledItem.GPIN = SelectedGPin;
            ledItem.BPIN = SelectedBPin;
            ledItem.CorrespondingArduino.BaudRate = SelectedBaud;

            //remove the properties window
            MainWindow.ActiveWindow.ShowProperties = false;

            //remove the eventhandler from the properties grid
            MainWindow.ActiveWindow.propertiesBackgroundGrid.MouseUp -= CancelBtn_Click;
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextBlock tb = (TextBlock)typeComboBox.SelectedItem;

            if (tb.Text == "4-PIN RGB")
            {
                argbPropertiesStackPanel.Visibility = Visibility.Collapsed;
                rgbPropertiesStackPanel.Visibility = Visibility.Visible;
            }
            else if (tb.Text == "3-PIN A-RGB")
            {
                argbPropertiesStackPanel.Visibility = Visibility.Visible;
                rgbPropertiesStackPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void DeleteItemBtn_Click(object sender, RoutedEventArgs e)
        {
            AlertWindow alert = new AlertWindow($"Are you sure you want to delete {ledItem.ItemName}?", true);
            alert.Owner = MainWindow.ActiveWindow;
            alert.ShowDialog();

            if (alert.DialogResult.Value)
            {
                ledItem.Delete();
                MainWindow.ActiveWindow.ShowProperties = false;
            }
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        //getters and setters
        public string[] AvailablePorts
        {
            get { return availablePorts; }
            set
            {
                availablePorts = value;
                OnPropertyChanged("AvailablePorts");
            }
        }

        public string SelectedComPort
        {
            get { return selectedComPort; }
            set
            {
                if (this.IsLoaded)
                    MadeChanges = true;

                selectedComPort = value;
                OnPropertyChanged("SelectedComPort");
            }
        }

        public string SelectedItemName
        {
            get { return selectedItemName; }
            set
            {
                if (this.IsLoaded)
                    MadeChanges = true;

                selectedItemName = value;
                OnPropertyChanged("SelectedItemName");
            }
        }

        public int SelectedType
        {
            get { return selectedType; }
            set
            {
                if (this.IsLoaded)
                    MadeChanges = true;

                selectedType = value;
                OnPropertyChanged("SelectedType");
            }
        }

        public int SelectedBaud
        {
            get { return selectedBaud; }
            set
            {
                if (this.IsLoaded)
                    MadeChanges = true;

                selectedBaud = value;
                OnPropertyChanged("SelectedBaud");
            }
        }

        public int SelectedLedCount
        {
            get { return selectedLedCount; }
            set
            {
                if (this.IsLoaded)
                    MadeChanges = true;

                selectedLedCount = value;
                OnPropertyChanged("SelectedLedCount");
            }
        }

        public int SelectedDPin
        {
            get { return selectedDPin; }
            set
            {
                if (this.IsLoaded)
                    MadeChanges = true;

                selectedDPin = value;
                OnPropertyChanged("SelectedDPin");
            }
        }

        public int SelectedRPin
        {
            get { return selectedRPin; }
            set
            {
                if (this.IsLoaded)
                    MadeChanges = true;

                selectedRPin = value;
                OnPropertyChanged("SelectedRPin");
            }
        }

        public int SelectedGPin
        {
            get { return selectedGPin; }
            set
            {
                if (this.IsLoaded)
                    MadeChanges = true;

                selectedGPin = value;
                OnPropertyChanged("SelectedGPin");
            }
        }

        public int SelectedBPin
        {
            get { return selectedBPin; }
            set
            {
                if (this.IsLoaded)
                    MadeChanges = true;

                selectedBPin = value;
                OnPropertyChanged("SelectedBPin");
            }
        }

        public bool MadeChanges
        {
            get { return madeChanges; }
            set 
            { 
                madeChanges = value;
                OnPropertyChanged("MadeChanges");
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
