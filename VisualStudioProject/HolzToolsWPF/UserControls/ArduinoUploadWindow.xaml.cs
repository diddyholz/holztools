using ArduinoUploader;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
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
using System.Net;
using System.IO;
using System.Threading;
using System.ComponentModel;
using Ionic.Zip;

namespace HolzTools.UserControls
{
    public partial class ArduinoUploadWindow : INotifyPropertyChanged
    {
        private ArduinoUploader.Hardware.ArduinoModel model;

        private string comPortText = "";
        private string modelText = "";
        private string fileName = "";
        private string progressText = "";
        private string errorText = "";

        private bool inputValid = false;
        private bool isFlashing = false;
        private bool isSuccessfull = false;
        private bool isNetwork = false;
        private bool isScanning = false;
        private bool noNetworkDevices = false;
        private bool isEsp32 = false;

        public ArduinoUploadWindow()
        {
            InitializeComponent();
            DataContext = this;

            MainWindow.ActiveWindow.uploadArduinoBinaryBackgroundGrid.MouseUp += CancelBtn_Click;

            comPortComboBox.ItemsSource = SerialPort.GetPortNames();
        }

        private void flashESP32USB()
        {
            IsFlashing = true;
            ProgressText = "Getting latest binary info";

            using (WebClient wc = new WebClient())
            {
                string[] downloadString = wc.DownloadString(MainWindow.ArduinoBinaryPasteBin).Split('|');

                string downloadUrl = "";

                //set the filename for the binary
                fileName = MainWindow.ActiveWindow.ArduinoBinaryDirectory + $@"binary{downloadString[0]}ESP.hex";

                //delete the file if it already exists
                if (File.Exists(fileName))
                    File.Delete(fileName);

                //set the correct model and downloadlink
                downloadUrl = downloadString[2];

                wc.DownloadFileCompleted += Wc_ESPUSBDownloadFileCompleted;

                ProgressText = "Downloading binary";

                wc.DownloadFileAsync(new Uri(downloadUrl), fileName);
            }
        }

        private void flashESP32OTA()
        {
            IsFlashing = true;
            ProgressText = "Getting latest binary info";

            using (WebClient wc = new WebClient())
            {
                string[] downloadString = wc.DownloadString(MainWindow.ArduinoBinaryPasteBin).Split('|');

                string downloadUrl = "";

                //set the filename for the binary
                fileName = MainWindow.ActiveWindow.ArduinoBinaryDirectory + $@"binary{downloadString[0]}ESP32.zip";

                //delete the file if it already exists
                if (File.Exists(fileName))
                    File.Delete(fileName);

                //set the correct model and downloadlink
                downloadUrl = downloadString[2];

                wc.DownloadFileCompleted += Wc_ESPOTADownloadFileCompleted;

                ProgressText = "Downloading binary";

                wc.DownloadFileAsync(new Uri(downloadUrl), fileName);
            }
        }

        private void flashArduino()
        {
            IsFlashing = true;
            ProgressText = "Getting latest binary info";

            using (WebClient wc = new WebClient())
            {
                string[] downloadString = wc.DownloadString(MainWindow.ArduinoBinaryPasteBin).Split('|');

                string downloadUrl = "";

                //set the filename for the binary
                fileName = MainWindow.ActiveWindow.ArduinoBinaryDirectory + $@"binary{downloadString[0]}Arduino.hex";

                //delete the file if it already exists
                if (File.Exists(fileName))
                    File.Delete(fileName);

                //set the correct model and downloadlink
                downloadUrl = downloadString[1];
                model = ArduinoUploader.Hardware.ArduinoModel.NanoR3;

                wc.DownloadFileCompleted += Wc_ArduinoDownloadFileCompleted;

                ProgressText = "Downloading binary";

                wc.DownloadFileAsync(new Uri(downloadUrl), fileName);
            }
        }

        //events
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.ShowArduinoUploadWindow = false;

            //remove the eventhandler from the uploadArduinoBinaryBackgroundGrid
            MainWindow.ActiveWindow.uploadArduinoBinaryBackgroundGrid.MouseUp -= CancelBtn_Click;
        }

        private void FlashBtn_Click(object sender, RoutedEventArgs e)
        {
            fileName = @"C:\Users\sidne\source\repos\HolzTools\ArduinoSketch\HolzToolsSketch\HolzToolsSketch.ino.eightanaloginputs.hex";

            if (arduinoModelComboBox.Text == "" || arduinoModelComboBox.Text == null)
            {
                ErrorText = "Select a model first";
                return;
            }

            if (comPortComboBox.Text == "" || comPortComboBox.Text == null)
            {
                ErrorText = "Select a COM-Port or IP first";
                return;
            }

            ErrorText = "";
            progressGrid.Visibility = Visibility.Visible;

            comPortText = comPortComboBox.Text;
            modelText = arduinoModelComboBox.Text;

            new Thread(new ThreadStart(() =>
            {
                if (!IsEsp32)
                    flashArduino();
                else if (IsEsp32 && IsNetwork)
                    flashESP32OTA();
                else if (IsEsp32 && !IsNetwork)
                    flashESP32USB();
            }))
            { ApartmentState = ApartmentState.STA }.Start();
        }

        private void Wc_ArduinoDownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            ArduinoSketchUploader uploader = new ArduinoSketchUploader(
                new ArduinoSketchUploaderOptions()
                {
                    FileName = fileName,
                    PortName = comPortText,
                    ArduinoModel = model
                });

            ProgressText = "Flashing binary";

            //try to close open COM-ports
            foreach (LedItem item in LedItem.AllItems)
            {
                if (item.ComPortName == comPortText)
                {
                    item.CloseSerial();
                }
            }

            //upload the binary
            try
            {
                uploader.UploadSketch();
            }
            catch (Exception ex)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    IsFlashing = false;
                    IsSuccessfull = false;
                    ProgressText = "Failed to upload binary";

                    MainWindow.ActiveWindow.logBoxText.Text += $"Failed to upload binary to model { modelText } at { comPortText } ({ ex.GetType().Name })";
                    MainWindow.ActiveWindow.logBoxText.Text += Environment.NewLine;
                }));
                return;
            }

            this.Dispatcher.BeginInvoke(new Action(() => {
                IsFlashing = false;
                IsSuccessfull = true;
                ProgressText = "Done";
            }));
        }

        private void Wc_ESPUSBDownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            //extract the zip to a tmp folder
            using (ZipFile zip = ZipFile.Read(fileName))
            {
                foreach (ZipEntry entry in zip)
                {
                    if (File.Exists(System.IO.Path.Combine(MainWindow.ActiveWindow.ArduinoBinaryDirectory, entry.FileName)))
                        File.Delete(System.IO.Path.Combine(MainWindow.ActiveWindow.ArduinoBinaryDirectory, entry.FileName));

                    entry.Extract(MainWindow.ActiveWindow.ArduinoBinaryDirectory);
                }
            }

            ProgressText = "Connecting";

            //try to close open COM-ports
            foreach (LedItem item in LedItem.AllItems)
            {
                if (item.ComPortName == comPortText)
                {
                    item.CloseSerial();
                }
            }

            //upload the binary
            try
            {
                byte hashVerified = 0;

                string cmd = $"/c \".\\python.exe esptool.py --chip esp32 --port {comPortText} --baud 460800 --before default_reset --after hard_reset write_flash -z --flash_mode dio --flash_freq 40m --flash_size detect 0x1000 {MainWindow.ActiveWindow.ArduinoBinaryDirectory}\\bootloader_dio_40m.bin 0x8000 {MainWindow.ActiveWindow.ArduinoBinaryDirectory}\\partitions.bin 0xe000 {MainWindow.ActiveWindow.ArduinoBinaryDirectory}\\boot_app0.bin 0x10000 {MainWindow.ActiveWindow.ArduinoBinaryDirectory}\\firmware.bin\"";

                System.Diagnostics.Process proc = new System.Diagnostics.Process();

                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = cmd;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.OutputDataReceived += (o, eventargs) => {
                    if (!String.IsNullOrEmpty(eventargs.Data))
                    {
                        if (eventargs.Data.Contains("Connecting."))
                        {
                            ProgressText = "Flashing";
                        }
                        else if (eventargs.Data.Contains("Hash of data verified."))
                        {
                            hashVerified++;
                        }
                    }
                };
                proc.Start();
                proc.BeginOutputReadLine();

                proc.WaitForExit();

                if (hashVerified < 4)
                {
                    this.Dispatcher.BeginInvoke(new Action(() => {
                        IsFlashing = false;
                        IsSuccessfull = false;
                        ProgressText = "Failed to upload binary";
                    }));

                    return;
                }
            }
            catch (Exception ex)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    IsFlashing = false;
                    IsSuccessfull = false;
                    ProgressText = "Failed to upload binary";

                    MainWindow.ActiveWindow.logBoxText.Text += $"Failed to upload binary to model { modelText } at { comPortText } ({ ex.GetType().Name })";
                    MainWindow.ActiveWindow.logBoxText.Text += Environment.NewLine;
                }));
                return;
            }

            this.Dispatcher.BeginInvoke(new Action(() => {
                IsFlashing = false;
                IsSuccessfull = true;
                ProgressText = "Done";
            }));
        }

        private void Wc_ESPOTADownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            //extract the zip to a tmp folder
            using (ZipFile zip = ZipFile.Read(fileName))
            {
                foreach (ZipEntry entry in zip)
                {
                    if (File.Exists(System.IO.Path.Combine(MainWindow.ActiveWindow.ArduinoBinaryDirectory, entry.FileName)))
                        File.Delete(System.IO.Path.Combine(MainWindow.ActiveWindow.ArduinoBinaryDirectory, entry.FileName));

                    entry.Extract(MainWindow.ActiveWindow.ArduinoBinaryDirectory);
                }
            }

            ProgressText = "Connecting";

            //upload the binary
            try
            {
                bool successful = false;

                string cmd = $"/c \".\\python.exe espota.py --debug --progress -i {comPortText.Substring(comPortText.IndexOf('(') + 1, comPortText.IndexOf(')') - comPortText.IndexOf('(') - 1)} -f {MainWindow.ActiveWindow.ArduinoBinaryDirectory}\\firmware.bin";

                System.Diagnostics.Process proc = new System.Diagnostics.Process();

                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = cmd;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.OutputDataReceived += (o, eventargs) => {
                    if (!String.IsNullOrEmpty(eventargs.Data))
                    {
                        if (eventargs.Data.Contains("Uploading"))
                        {
                            ProgressText = "Flashing";
                        }
                        else if (eventargs.Data.Contains("Success"))
                        {
                            successful = true;
                        }
                    }
                };
                proc.Start();
                proc.BeginOutputReadLine();

                proc.WaitForExit();

                if (!successful)
                {
                    this.Dispatcher.BeginInvoke(new Action(() => {
                        IsFlashing = false;
                        IsSuccessfull = false;
                        ProgressText = "Failed to upload binary";
                    }));

                    return;
                }
            }
            catch (Exception ex)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    IsFlashing = false;
                    IsSuccessfull = false;
                    ProgressText = "Failed to upload binary";

                    MainWindow.ActiveWindow.logBoxText.Text += $"Failed to upload binary to model { modelText } at { comPortText } ({ ex.GetType().Name })";
                    MainWindow.ActiveWindow.logBoxText.Text += Environment.NewLine;
                }));
                return;
            }

            this.Dispatcher.BeginInvoke(new Action(() => {
                IsFlashing = false;
                IsSuccessfull = true;
                ProgressText = "Done";
            }));
        }

        private void arduinoModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (arduinoModelComboBox.SelectedItem == null || comPortComboBox.SelectedItem == null)
                InputValid = false;
            else
                InputValid = true;

            // check if the selected controller is updated via ota
            if ((arduinoModelComboBox.SelectedItem as ComboBoxItem).Content.ToString() == "ESP32")
            {
                IsNetwork = true;
                IsEsp32 = true;

                IsScanning = true;

                List<string> devices = new List<string>();

                Thread discoverDevicesThread = new Thread(() =>
                {
                    devices = MainWindow.GetNetworkDevices();

                    // check if the model is still selected
                    this.Dispatcher.Invoke(() =>
                    {
                        if (IsNetwork)
                            comPortComboBox.ItemsSource = devices;
                        else
                            comPortComboBox.ItemsSource = SerialPort.GetPortNames();
                    });


                    IsScanning = false;
                });

                discoverDevicesThread.Start();
            }
            else
            {
                IsNetwork = false;
                IsEsp32 = false;

                comPortComboBox.ItemsSource = SerialPort.GetPortNames();
            }
        }

        private void comPortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (arduinoModelComboBox.SelectedItem == null || comPortComboBox.SelectedItem == null)
                InputValid = false;
            else
                InputValid = true;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsNetwork = false;

            // get all com ports
            comPortComboBox.ItemsSource = SerialPort.GetPortNames();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsNetwork = true;

            // scan for network devices
            IsScanning = true;

            List<string> devices = new List<string>();

            Thread discoverDevicesThread = new Thread(() =>
            {
                devices = MainWindow.GetNetworkDevices();

                // check if the model is still selected
                this.Dispatcher.Invoke(() =>
                {
                    if (IsNetwork)
                        comPortComboBox.ItemsSource = devices;
                    else
                        comPortComboBox.ItemsSource = SerialPort.GetPortNames();
                });

                IsScanning = false;
            });

            discoverDevicesThread.Start();
        }

        //getters and setters
        public bool InputValid
        {
            get { return inputValid; }
            set
            {
                inputValid = value;
                OnPropertyChanged("InputValid");
            }
        }

        public bool IsSuccessfull
        {
            get { return isSuccessfull; }
            set
            {
                isSuccessfull = value;
                OnPropertyChanged("IsSuccessfull");
            }
        }

        public bool IsFlashing
        {
            get { return isFlashing; }
            set
            {
                isFlashing = value;
                OnPropertyChanged("IsFlashing");
            }
        }

        public bool IsNetwork
        {
            get { return isNetwork; }
            set
            {
                isNetwork = value;
                OnPropertyChanged("IsNetwork");
            }
        }

        public bool IsScanning
        {
            get { return isScanning; }
            set
            {
                isScanning = value;
                OnPropertyChanged("IsScanning");
            }
        }

        public bool IsEsp32
        {
            get { return isEsp32; }
            set
            {
                isEsp32 = value;
                OnPropertyChanged("IsEsp32");
            }
        }

        public bool NoNetworkDevices
        {
            get { return noNetworkDevices; }
            set
            {
                noNetworkDevices = value;
                OnPropertyChanged("NoNetworkDevices");
            }
        }

        public string ProgressText
        {
            get { return progressText; }
            set
            {
                progressText = value;
                OnPropertyChanged("ProgressText");
            }
        }

        public string ErrorText
        {
            get { return errorText; }
            set
            {
                errorText = value;
                OnPropertyChanged("ErrorText");
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

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
