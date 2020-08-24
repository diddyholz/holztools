﻿using ArduinoUploader;
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

        public ArduinoUploadWindow()
        {
            InitializeComponent();
            DataContext = this;

            comPortComboBox.ItemsSource = SerialPort.GetPortNames();

            MainWindow.ActiveWindow.uploadArduinoBinaryBackgroundGrid.MouseUp += CancelBtn_Click;
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
                ErrorText = "Select a COM-Port first";
                return;
            }

            ErrorText = "";
            progressGrid.Visibility = Visibility.Visible;

            comPortText = comPortComboBox.Text;
            modelText = arduinoModelComboBox.Text;

            new Thread(new ThreadStart(() =>
            {
                model = ArduinoUploader.Hardware.ArduinoModel.UnoR3;

                IsFlashing = true;
                ProgressText = "Getting latest binary info";

                using (WebClient wc = new WebClient())
                {
                    string[] downloadString = wc.DownloadString(MainWindow.ArduinoBinaryPasteBin).Split('|');

                    string downloadUrl = "";

                    //set the filename for the binary
                    fileName = MainWindow.ActiveWindow.ArduinoBinaryDirectory + $@"binary{downloadString[0]}.hex";

                    //delete the file if it already exists
                    if (File.Exists(fileName))
                        File.Delete(fileName);

                    //set the correct model and downloadlink
                    switch (modelText)
                    {
                        case "Arduino Nano (R3)":
                            downloadUrl = downloadString[1];
                            model = ArduinoUploader.Hardware.ArduinoModel.NanoR3;
                            break;

                        case "Arduino Uno (R3)":
                            downloadUrl = downloadString[2];
                            model = ArduinoUploader.Hardware.ArduinoModel.UnoR3;
                            break;
                    }

                    wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                    wc.DownloadFileCompleted += Wc_DownloadFileCompleted;

                    ProgressText = "Downloading binary";

                    wc.DownloadFileAsync(new Uri(downloadUrl), fileName);
                }
            }))
            { ApartmentState = ApartmentState.STA }.Start();
        }

        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
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
            foreach(LedItem item in LedItem.AllItems)
            {
                if(item.ComPortName == comPortText)
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

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {

        }

        private void arduinoModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (arduinoModelComboBox.SelectedItem == null || comPortComboBox.SelectedItem == null)
                InputValid = false;
            else
                InputValid = true;
        }

        private void comPortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (arduinoModelComboBox.SelectedItem == null || comPortComboBox.SelectedItem == null)
                InputValid = false;
            else
                InputValid = true;
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
}
