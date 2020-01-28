using HolzTools.UserControls;
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
using System.Windows.Threading;

namespace HolzTools.ModeControls
{
    public partial class ModeMusic : INotifyPropertyChanged
    {
        private string[] selectedDevice;
        
        private byte intensity;

        private Analyzer analyzer;

        public ModeMusic()
        {
            InitializeComponent();
            DataContext = this;
            Intensity = 0;
        }

        public void InitAnalyzer()
        {
            analyzer = new Analyzer(soundDevicesList);
            analyzer.GetDevices();
            analyzer.Init();
            OnPropertyChanged("SoundAnalyzer");
        }

        public void SetIntensity(List<byte> data)
        {
            if (data.Count < 16) return;
        
            //send the sound intensity value to all items using music mode
            foreach(LedItem item in LedItem.AllItems)
            {
                if(item.CurrentMode == "Music")
                {
                    //each item can have their own musicFrequency setting
                    item.SerialWrite($"+{ data[item.MusicFrequency] }\\n");
                }
            }

            //set the intensity for the preview
            Intensity = data[MusicFrequency];
        }

        private void Analyzer_InitFinished(object sender, EventArgs e)
        {
            MessageBox.Show("Successfully loaded Bass.net");
        }

        //events
        private void SoundDevicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedDevice = (soundDevicesList.Items[soundDevicesList.SelectedIndex] as string).Split('-');

            MainWindow.ActiveWindow.MadeChanges = true;
        }

        private void StartBassNetBtn_Click(object sender, RoutedEventArgs e)
        {
            new AlertWindow($"While BASS.NET is starting, {MainWindow.ApplicationName} will get unresponsive.\nDon't worry and have patience.").ShowDialog();
            InitAnalyzer();
        }

        //getters and setters
        public Analyzer SoundAnalyzer
        {
            get { return analyzer; }
            set { analyzer = value; }
        }

        public string OverlappedMode
        {
            get
            {
                if (MainWindow.ActiveWindow.SelectedLedItem == null) return null;

                return MainWindow.ActiveWindow.SelectedLedItem.OverlappedMusicMode;
            }
            set
            {
                MainWindow.ActiveWindow.SelectedLedItem.OverlappedMusicMode = value;
                MainWindow.ActiveWindow.MadeChanges = true;
                OnPropertyChanged("OverlappedMode");
            }
        }
        public string[] SelectedDevice
        {
            get { return selectedDevice; }
        }

        public byte Intensity
        {
            get { return intensity; }
            set
            {
                intensity = value;
                OnPropertyChanged("Intensity");
            }
        }

        public int MusicFrequency
        {
            get
            {
                if (MainWindow.ActiveWindow.SelectedLedItem == null) return 0;

                return MainWindow.ActiveWindow.SelectedLedItem.MusicFrequency;
            }
            set
            {
                MainWindow.ActiveWindow.SelectedLedItem.MusicFrequency = value;
                OnPropertyChanged("MusicFrequency");
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
