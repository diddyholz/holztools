using HolzTools.UserControls;
using ArduinoUploader;
using AutoUpdate;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml;

namespace HolzTools
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        public static MainWindow activeWindow = null;

        public static string InstallLocation = "";

        private const string currentVersion = "1.00";
        private const string updatePasteBin = "https://pastebin.com/raw/t2r0pWMr";
        private const string changelogPasteBin = "https://pastebin.com/raw/mQK7VVGZ";
        private const string arduinoBinaryPasteBin = "https://pastebin.com/raw/eAYERLEs";
        private const string arduinoBinaryChangelogPasteBin = "https://pastebin.com/raw/VHdBVP3b";
        private const string appName = "HolzTools";
        private const string guid = "{1B3CB67C-6ADF-46DA-8740-752AFC4BA540}";

        private static int idCounter = 0;

        private bool isDev = false;
        private bool showProperties = false;
        private bool showSettings = false;
        private bool showMainGrid = false;
        private bool showColorPicker = false;
        private bool showArduinoUploadWindow = false;
        private bool autoUpdate = true;
        private bool madeChanges = false;
        private bool blockPopups = false;
        private bool startBassNet = true;
        private bool isMinimized = false;
        private bool enableFanAnim = false;

        private double lastTop = 0.00;

        private byte loadingProgress = 0;
        private byte iconPressed = 0;

        private string selectedMode = "Static";

        private Color accentColor = Color.FromRgb(200, 0, 0);

        private LedItem selectedItem = null;

        private Settings settingsWindow;

        private ArduinoBinaryDownloaderWindow arduinoBinaryDownloadWindow;

        #region Stuff to maximize properly

        private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }
            return (IntPtr)0;
        }

        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>x coordinate of point.</summary>
            public int x;
            /// <summary>y coordinate of point.</summary>
            public int y;
            /// <summary>Construct a point of coordinates (x,y).</summary>
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            public static readonly RECT Empty = new RECT();
            public int Width { get { return Math.Abs(right - left); } }
            public int Height { get { return bottom - top; } }
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
            public RECT(RECT rcSrc)
            {
                left = rcSrc.left;
                top = rcSrc.top;
                right = rcSrc.right;
                bottom = rcSrc.bottom;
            }
            public bool IsEmpty { get { return left >= right || top >= bottom; } }
            public override string ToString()
            {
                if (this == Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }
            public override bool Equals(object obj)
            {
                if (!(obj is Rect)) { return false; }
                return (this == (RECT)obj);
            }
            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode() => left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2) { return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom); }
            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2) { return !(rect1 == rect2); }
        }

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        #endregion

        public MainWindow()
        {
            //get and set the installLocation of the program
            InstallLocation = Assembly.GetEntryAssembly().Location;

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + guid, true))
                {
                    key.SetValue("DisplayName", ApplicationName, RegistryValueKind.String);
                    key.SetValue("InstallLocation", InstallLocation, RegistryValueKind.String);
                    key.SetValue("DisplayIcon", Path.Combine(InstallLocation, "icon.ico"));
                }
            }
            catch(Exception ex)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    logBoxText.Text += $"Could not open UninstallRegistryKey({ex.GetType().Name})";
                    logBoxText.Text += Environment.NewLine;
                }));
            }

            activeWindow = this;

            InitializeComponent();

            DataContext = this;

            SourceInitialized += (s, e) =>
            {
                IntPtr handle = (new WindowInteropHelper(this)).Handle;
                HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WindowProc));
            };

            Thread applicationStartThread = new Thread(() => applicationStart(App.Args));
            applicationStartThread.IsBackground = true;
            applicationStartThread.SetApartmentState(ApartmentState.STA);
            applicationStartThread.Start();
        }

        public void CheckForUpdate(bool notifyIfNoUpdate = false)
        {
            UpdateWindow updateWindow = new UpdateWindow();

            Update update = new Update(currentVersion, updatePasteBin, Process.GetCurrentProcess().Id, updateWindow.updateProgressBar, updateWindow.updateTextBlock, updateWindow);

            //check for internet connection
            bool connectionAvailable = Update.CheckForInternet();

            if (connectionAvailable)
            {

                //check for a new update
                if (update.CheckForUpdate())
                {
                    AlertWindow updateAlert = new AlertWindow("An update is available. Do you want to update?", true);
                    updateAlert.ShowDialog();

                    if (updateAlert.DialogResult.Value)
                    {
                        update.Upgrade();
                    }
                }
                else
                {
                    if (notifyIfNoUpdate)
                        new AlertWindow($"{ApplicationName} is on the newest version.").ShowDialog();
                }

                using (WebClient wc = new WebClient())
                {
                    string newestVersion = wc.DownloadString(arduinoBinaryPasteBin).Split('|')[0];

                    bool foundUpdate = false;

                    List<Arduino> updatableArduinos = new List<Arduino>();

                    //check if arduinos can be updated
                    foreach (Arduino arduino in Arduino.AllArduinos)
                    {
                        if (arduino.BinaryVersion == "")
                        {
                            new AlertWindow($"Could not receive the binary Information of your Arduino at { arduino.SerialPortName }!\n" +
                                $"If this error keeps coming up, try to replug your arduino or reflash its binary.").ShowDialog();
                        }
                        else if (arduino.BinaryVersion != newestVersion)
                        {
                            foundUpdate = true;
                            updatableArduinos.Add(arduino);
                        }
                    }

                    if (foundUpdate)
                    {
                        AlertWindow alert = new AlertWindow($"An update for { updatableArduinos.Count } of your Arduino/s is available. Do you want to install it now? " +
                            $"Your LEDs might not continue to work properly if you don't update.", true);

                        alert.ShowDialog();

                        if (alert.DialogResult.Value)
                        {
                            string[] downloadString = wc.DownloadString(MainWindow.ArduinoBinaryPasteBin).Split('|');

                            string fileName = "";

                            //install the binary for the arduinos
                            foreach (Arduino arduino in updatableArduinos)
                            {
                                string downloadUrl = "";

                                bool finishedDownloading = false;

                                //close the serialport to give the uploader access to the comport
                                arduino.ActiveSerialPort.Close();

                                //set the filename for the binary
                                fileName = ArduinoBinaryDirectory + $@"binary{downloadString[0]}m{arduino.ArduinoType}.hex";

                                //delete the file if it already exists
                                if (File.Exists(fileName))
                                    File.Delete(fileName);

                                //set the correct downloadlink
                                switch (arduino.ArduinoType)
                                {
                                    case Arduino.Type.NanoR3:
                                        downloadUrl = downloadString[1];
                                        break;

                                    case Arduino.Type.UnoR3:
                                        downloadUrl = downloadString[2];
                                        break;
                                }

                                if (downloadUrl == "")
                                {
                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        logBoxText.Text += $"Couldn't get a valid type for Arduino at {arduino.SerialPortName}";
                                        logBoxText.Text += Environment.NewLine;
                                    }));

                                    break;
                                }

                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    //create a window that shows the status of the flashing progress
                                    arduinoBinaryDownloadWindow = new ArduinoBinaryDownloaderWindow();
                                    arduinoBinaryDownloadWindow.ShowDialog();
                                }));

                                wc.DownloadProgressChanged += (o, e) =>
                                {
                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        float progress = (float)e.BytesReceived / (float)e.TotalBytesToReceive;

                                        arduinoBinaryDownloadWindow.updateProgressBar.Value = (int)(progress * 100);
                                    }));
                                };

                                wc.DownloadFileCompleted += (o, e) =>
                                {
                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        arduinoBinaryDownloadWindow.statusTextBlock.Text = "Flashing Binary";
                                    }));
                                    finishedDownloading = true;
                                };

                                wc.DownloadFileAsync(new Uri(downloadUrl), fileName);

                                //wait for the download to finish
                                while (!finishedDownloading) { Thread.Sleep(100); }

                                //upload the binary
                                try
                                {
                                    ArduinoSketchUploader uploader = new ArduinoSketchUploader(
                                        new ArduinoSketchUploaderOptions()
                                        {
                                            FileName = fileName,
                                            PortName = arduino.SerialPortName,
                                            ArduinoModel = (ArduinoUploader.Hardware.ArduinoModel)arduino.ArduinoType
                                        });

                                    uploader.UploadSketch();

                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        logBoxText.Text += $"Finished flashing of binary for model { arduino.ArduinoType } at { arduino.SerialPortName }";
                                        logBoxText.Text += Environment.NewLine;
                                    }));
                                }
                                catch (Exception ex)
                                {
                                    this.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        new AlertWindow("Failed to upload binary!", false).ShowDialog();

                                        logBoxText.Text += $"Failed to upload binary to model { arduino.ArduinoType } at { arduino.SerialPortName } ({ ex.GetType().Name })";
                                        logBoxText.Text += Environment.NewLine;
                                    }));
                                }

                                this.Dispatcher.BeginInvoke(new Action(() => arduinoBinaryDownloadWindow.Close()));
                            }

                            //display the changelog for the newer binary
                            new ChangeLogWindow(wc.DownloadString(arduinoBinaryChangelogPasteBin), newestVersion).ShowDialog();

                            //set every led mode again
                            setEveryLedMode(false);
                        }
                    }
                    else
                    {
                        if (notifyIfNoUpdate)
                        {
                            if(Arduino.AllArduinos.Count == 1)
                                new AlertWindow("The binary on your Arduino is on the newest version.").ShowDialog();
                            else
                                new AlertWindow("The binary on your Arduinos is on the newest version.").ShowDialog();
                        }
                    }
                }
            }
            else if (!connectionAvailable)
            {
                new AlertWindow("Couldn't check for updates!", false).ShowDialog();

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    logBoxText.Text += $"Couldn't check for updates (Unable to connect to 'http://google.com/generate_204')";
                    logBoxText.Text += Environment.NewLine;
                }));
            }
        }

        private void applicationStart(string[] args)
        {
            bool startup = false;

            if (args != null)
            {
                foreach (string s in args)
                {
                    if (s == "-startup")
                        startup = true;
                }
            }

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                loadingText.Text = "Loading Settings";
                LoadingProgress = 10;
            }));

            //load the program
            this.Dispatcher.Invoke(new Action(() => loadProgram()));

            //check if an installer is downloaded and get delete it
            if (Directory.Exists(Update.InstallerLocation))
            {
                Directory.Delete(Update.InstallerLocation, true);
                new Thread(getChangelogThread) { ApartmentState = ApartmentState.STA, IsBackground = true }.Start();
            }

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                loadingText.Text = "Starting connected LEDs";
                LoadingProgress = 20;
            }));

            //set the mode of every leditem
            setEveryLedMode(true);

            //check for updates
            if (AutoUpdate)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    loadingText.Text = "Checking for Updates";
                    LoadingProgress = 50;
                }));

                CheckForUpdate();
            }

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                loadingText.Text = "Starting BASS.NET";
                LoadingProgress = 70;
            }));

            if (StartBassNet)
            {
                Thread.Sleep(100);

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    modeMusic.InitAnalyzer();
                }));
            }

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                loadingText.Text = "Finishing";
                LoadingProgress = 100;
                SelectedLedItem = SelectedLedItem;
                ShowMainGrid = true;
                MadeChanges = false;
            }));

            if (startup)
            {
                this.Dispatcher.BeginInvoke(new Action(() => this.Hide()));
                isMinimized = true;
            }
        }

        private void setEveryLedMode(bool getInformation)
        {
            foreach (LedItem item in LedItem.AllItems)
            {
                //send the data to the arduino (the delays have to be there because else the arduino wouldn't have enough time to start a serial connection between the pc and arduino)
                try
                {
                    item.InitSerial();
                }
                catch (Exception ex)
                {
                    AlertWindow alert = new AlertWindow($"Can't write to {item.ComPortName}", false);
                    alert.ShowDialog();

                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        logBoxText.Text += $"Can't write to {item.ComPortName} ({ ex.GetType().Name })";
                        logBoxText.Text += Environment.NewLine;
                    }));

                    continue;
                }

                Thread.Sleep(1500);

                //set every mode argument for the current leditem
                this.Dispatcher.Invoke(() =>
                {
                    modeStatic.Brightness = item.StaticBrightness;
                    modeCycle.Brightness = item.CycleBrightness;
                    modeRainbow.Brightness = item.RainbowBrightness;
                    modeLightning.Brightness = item.LightningBrightness;
                    modeCycle.Speed = item.CycleSpeed;
                    modeRainbow.Speed = item.RainbowSpeed;
                    modeOverlay.Speed = item.OverlaySpeed;
                    modeSpinner.Speed = item.SpinnerSpeed;
                    modeOverlay.Direction = item.OverlayDirection;
                    modeStatic.SelectedColor = item.StaticModeColor;
                    modeLightning.SelectedColor = item.LightningModeColor;
                    modeSpinner.SpinnerColor = item.SpinnerModeSpinnerColor;
                    modeSpinner.BackgroundColor = item.SpinnerModeBackgroundColor;
                    modeSpinner.Length = item.SpinnerLength;
                    modeSpinner.SpinnerColorBrightness = item.SpinnerModeSpinnerColorBrightness;
                    modeSpinner.BackgroundColorBrightness = item.SpinnerModeBackgroundColorBrightness;
                });

                sendDataToArduino(item, item.CurrentMode, false);
                Thread.Sleep(400);
                if (getInformation) getArduinoInformation(item.CorrespondingArduino);
                Thread.Sleep(200);
            }

            //reset the mode arguments
            this.Dispatcher.Invoke(() => SelectedLedItem = SelectedLedItem);
        }

        private void saveProgram()
        {
            //write the program save to xml
            XmlTextWriter xml = new XmlTextWriter(SettingsFileFullPath, Encoding.UTF8);

            xml.Formatting = Formatting.Indented;

            xml.WriteStartElement("Settings");

            xml.WriteStartElement("Items");

            //save every led
            foreach (UserControls.LedItem item in UserControls.LedItem.AllItems)
            {
                xml.WriteStartElement("Item");
                xml.WriteAttributeString("name", item.Name);

                xml.WriteStartElement("ItemName");
                xml.WriteString(item.ItemName);
                xml.WriteEndElement();

                xml.WriteStartElement("BaudRate");
                xml.WriteString(item.BaudRate.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("ComPortName");
                xml.WriteString(item.ComPortName);
                xml.WriteEndElement();

                xml.WriteStartElement("Type");
                xml.WriteString(item.Type.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("LEDCount");
                xml.WriteString(item.LedCount.ToString());
                xml.WriteEndElement();

                //save every Pin
                xml.WriteStartElement("DataPin");
                xml.WriteString(item.DPIN.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("RedPin");
                xml.WriteString(item.RPIN.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("GreenPin");
                xml.WriteString(item.GPIN.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("BluePin");
                xml.WriteString(item.BPIN.ToString());
                xml.WriteEndElement();

                //current selected mode
                xml.WriteStartElement("CurrentMode");
                xml.WriteString(item.CurrentMode);
                xml.WriteEndElement();

                //save the mode Arguments
                xml.WriteStartElement("StaticBrightness");
                xml.WriteString(item.StaticBrightness.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CycleBrightness");
                xml.WriteString(item.CycleBrightness.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("RainbowBrightness");
                xml.WriteString(item.RainbowBrightness.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("LightningBrightness");
                xml.WriteString(item.LightningBrightness.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CycleSpeed");
                xml.WriteString(item.CycleSpeed.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("RainbowSpeed");
                xml.WriteString(item.RainbowSpeed.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("OverlaySpeed");
                xml.WriteString(item.OverlaySpeed.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpinnerSpeed");
                xml.WriteString(item.SpinnerSpeed.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpinnerLength");
                xml.WriteString(item.SpinnerLength.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("OverlayDirection");
                xml.WriteString(item.OverlayDirection.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("StaticModeColorRed");
                xml.WriteString(item.StaticModeColor.R.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("StaticModeColorGreen");
                xml.WriteString(item.StaticModeColor.G.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("StaticModeColorBlue");
                xml.WriteString(item.StaticModeColor.B.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("LightningModeColorRed");
                xml.WriteString(item.LightningModeColor.R.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("LightningModeColorGreen");
                xml.WriteString(item.LightningModeColor.G.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("LightningModeColorBlue");
                xml.WriteString(item.LightningModeColor.B.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpinnerModeSpinnerColorRed");
                xml.WriteString(item.SpinnerModeSpinnerColor.R.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpinnerModeSpinnerColorGreen");
                xml.WriteString(item.SpinnerModeSpinnerColor.G.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpinnerModeSpinnerColorBlue");
                xml.WriteString(item.SpinnerModeSpinnerColor.B.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpinnerModeBackgroundColorRed");
                xml.WriteString(item.SpinnerModeBackgroundColor.R.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpinnerModeBackgroundColorGreen");
                xml.WriteString(item.SpinnerModeBackgroundColor.G.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpinnerModeBackgroundColorBlue");
                xml.WriteString(item.SpinnerModeBackgroundColor.B.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpinnerModeBackgroundColorBrightness");
                xml.WriteString(item.SpinnerModeBackgroundColorBrightness.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpinnerModeSpinnerColorBrightness");
                xml.WriteString(item.SpinnerModeSpinnerColorBrightness.ToString());
                xml.WriteEndElement();

                //on status
                xml.WriteStartElement("On");
                xml.WriteString(item.IsOn.ToString());
                xml.WriteEndElement();

                //overlaped mode
                xml.WriteStartElement("OverlappedMusicMode");
                xml.WriteString(item.OverlappedMusicMode);
                xml.WriteEndElement();

                xml.WriteEndElement();
            }

            xml.WriteEndElement();

            //save the app settings
            xml.WriteStartElement("AppPreferences");

            xml.WriteStartElement("IsDev");
            xml.WriteString(IsDev.ToString());
            xml.WriteEndElement();

            xml.WriteStartElement("EnableLogBox");
            xml.WriteString(EnableLogBox.ToString());
            xml.WriteEndElement();

            xml.WriteStartElement("StartBassNet");
            xml.WriteString(StartBassNet.ToString());
            xml.WriteEndElement();

            xml.WriteStartElement("BlockPopups");
            xml.WriteString(BlockPopups.ToString());
            xml.WriteEndElement();

            xml.WriteStartElement("AutoUpdate");
            xml.WriteString(AutoUpdate.ToString());
            xml.WriteEndElement();

            xml.WriteStartElement("EnableFanAnim");
            xml.WriteString(EnableFanAnim.ToString());
            xml.WriteEndElement();

            xml.WriteStartElement("AccentRed");
            xml.WriteString(AccentColor.R.ToString());
            xml.WriteEndElement();

            xml.WriteStartElement("AccentGreen");
            xml.WriteString(AccentColor.G.ToString());
            xml.WriteEndElement();

            xml.WriteStartElement("AccentBlue");
            xml.WriteString(AccentColor.B.ToString());
            xml.WriteEndElement();

            xml.WriteEndElement();

            xml.Close();
        }
        
        private void loadProgram()
        {
            //clear everything 
            try
            {
                itemStackPanel.Children.Clear();
                LedItem.AllItems.Clear();
            }
            catch { }

            //open the xml file
            XmlDocument xml = new XmlDocument();

            xml.Load(SettingsFileFullPath);

            //load the settings
            foreach (XmlNode node in xml.DocumentElement)
            {
                if (node.Name == "Items")
                {
                    //add the items with their settings
                    foreach (XmlNode itemNode in node)
                    {
                        string name = null;
                        string mode = null;
                        string comPort = "COM 0";
                        string overlappedMusicMode = null;

                        int baudRate = 4800;

                        byte type = 0;
                        byte ledCount = 0;
                        byte dPin = 0;
                        byte rPin = 0;
                        byte gPin = 0;
                        byte bPin = 0;

                        byte staticBrightness = 255;
                        byte cycleBrightness = 255;
                        byte rainbowBrightness = 255;
                        byte lightningBrightness = 255;
                        byte cycleSpeed = 0;
                        byte rainbowSpeed = 0;
                        byte overlaySpeed = 0;
                        byte spinnerSpeed = 0;
                        byte overlayDirection = 0;
                        byte spinnerLength = 0;
                        byte spinnerModeSpinnerColorBrightness = 0;
                        byte spinnerModeBackgroundColorBrightness = 0;

                        bool on = true;

                        Color staticModeColor = Color.FromRgb(255, 0, 0);
                        Color lightningModeColor = Color.FromRgb(255, 0, 0);
                        Color spinnerModeSpinnerColor = Color.FromRgb(255, 0, 0);
                        Color spinnerModeBackgroundColor = Color.FromRgb(255, 255, 255);

                        //get the item properties
                        foreach (XmlNode valueNode in itemNode)
                        {
                            if (valueNode.Name == "ItemName")
                            {
                                name = valueNode.InnerText;
                            }
                            else if (valueNode.Name == "Type")
                            {
                                type = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "ComPortName")
                            {
                                comPort = valueNode.InnerText;
                            }
                            else if (valueNode.Name == "BaudRate")
                            {
                                baudRate = Convert.ToInt32(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "LEDCount")
                            {
                                ledCount = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "DataPin")
                            {
                                dPin = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "RedPin")
                            {
                                rPin = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "GreenPin")
                            {
                                gPin = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "BluePin")
                            {
                                bPin = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "CurrentMode")
                            {
                                mode = valueNode.InnerText;
                            }
                            else if (valueNode.Name == "StaticBrightness")
                            {
                                staticBrightness = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "CycleBrightness")
                            {
                                cycleBrightness = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "RainbowBrightness")
                            {
                                rainbowBrightness = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "LightningBrightness")
                            {
                                lightningBrightness = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "CycleSpeed")
                            {
                                cycleSpeed = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "RainbowSpeed")
                            {
                                rainbowSpeed = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "OverlaySpeed")
                            {
                                overlaySpeed = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "SpinnerSpeed")
                            {
                                spinnerSpeed = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "OverlayDirection")
                            {
                                overlayDirection = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "SpinnerLength")
                            {
                                spinnerLength = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "StaticModeColorRed")
                            {
                                staticModeColor.R = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "StaticModeColorGreen")
                            {
                                staticModeColor.G = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "StaticModeColorBlue")
                            {
                                staticModeColor.B = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "LightningModeColorRed")
                            {
                                lightningModeColor.R = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "LightningModeColorGreen")
                            {
                                lightningModeColor.G = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "LightningModeColorBlue")
                            {
                                lightningModeColor.B = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "SpinnerModeSpinnerColorRed")
                            {
                                spinnerModeSpinnerColor.R = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "SpinnerModeSpinnerColorGreen")
                            {
                                spinnerModeSpinnerColor.G = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "SpinnerModeSpinnerColorBlue")
                            {
                                spinnerModeSpinnerColor.B = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "SpinnerModeBackgroundColorRed")
                            {
                                spinnerModeBackgroundColor.R = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "SpinnerModeBackgroundColorGreen")
                            {
                                spinnerModeBackgroundColor.G = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "SpinnerModeBackgroundColorBlue")
                            {
                                spinnerModeBackgroundColor.B = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "SpinnerModeSpinnerColorBrightness")
                            {
                                spinnerModeSpinnerColorBrightness = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "SpinnerModeBackgroundColorBrightness")
                            {
                                spinnerModeBackgroundColorBrightness = Convert.ToByte(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "On")
                            {
                                on = Convert.ToBoolean(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "OverlappedMusicMode")
                            {
                                overlappedMusicMode = valueNode.InnerText;
                            }
                            else
                            {
                                MessageBox.Show("Error while loading the settings file. Although the program should still work correctly, it is recommended to reset your settings.");
                            }
                        }

                        //create the item 
                        LedItem item = new LedItem()
                        {
                            Name = itemNode.Attributes[0].InnerText,
                            Type = type,
                            ComPortName = comPort,
                            BaudRate = baudRate,
                            LedCount = ledCount,
                            DPIN = dPin,
                            RPIN = rPin,
                            GPIN = gPin,
                            BPIN = bPin,
                            ItemName = name,
                            CurrentMode = mode,
                            IsOn = on,
                            OverlappedMusicMode = overlappedMusicMode,
                            StaticBrightness = staticBrightness,
                            CycleBrightness = cycleBrightness,
                            RainbowBrightness = rainbowBrightness,
                            LightningBrightness = lightningBrightness,
                            CycleSpeed = cycleSpeed,
                            RainbowSpeed = rainbowSpeed,
                            OverlaySpeed = overlaySpeed,
                            SpinnerSpeed = spinnerSpeed,
                            OverlayDirection = overlayDirection,
                            SpinnerLength = spinnerLength,
                            StaticModeColor = staticModeColor,
                            LightningModeColor = lightningModeColor,
                            SpinnerModeSpinnerColor = spinnerModeSpinnerColor,
                            SpinnerModeBackgroundColor = spinnerModeBackgroundColor,
                            SpinnerModeSpinnerColorBrightness = spinnerModeSpinnerColorBrightness,
                            SpinnerModeBackgroundColorBrightness = spinnerModeBackgroundColorBrightness
                        };
                    }
                }
                else if (node.Name == "AppPreferences")
                {
                    byte red = 255;
                    byte green = 0;
                    byte blue = 0;

                    //get the settings for the app
                    foreach (XmlNode subNode in node)
                    {
                        if (subNode.Name == "EnableLogBox")
                        {
                            EnableLogBox = subNode.InnerText == "True";
                        }
                        else if (subNode.Name == "IsDev")
                        {
                            IsDev = subNode.InnerText == "True";
                        }
                        else if (subNode.Name == "AutoUpdate")
                        {
                            AutoUpdate = subNode.InnerText == "True";
                        }
                        else if (subNode.Name == "EnableFanAnim")
                        {
                            EnableFanAnim = subNode.InnerText == "True";
                        }
                        else if (subNode.Name == "BlockPopups")
                        {
                            BlockPopups = subNode.InnerText == "True";
                        }
                        else if (subNode.Name == "StartBassNet")
                        {
                            StartBassNet = subNode.InnerText == "True";
                        }
                        else if (subNode.Name == "AccentRed")
                        {
                            red = Convert.ToByte(subNode.InnerText);
                        }
                        else if (subNode.Name == "AccentGreen")
                        {
                            green = Convert.ToByte(subNode.InnerText);
                        }
                        else if (subNode.Name == "AccentBlue")
                        {
                            blue = Convert.ToByte(subNode.InnerText);
                        }
                    }

                    AccentColor = Color.FromRgb(red, green, blue);
                }
                else
                {
                    MessageBox.Show("Error while loading the settings file. Although the program should still work correctly, it is recommended to reset your settings.");
                }
            }

            //select the correct mode
            if (SelectedLedItem != null)
                SelectedMode = SelectedLedItem.CurrentMode;

            MadeChanges = false;
        }

        private void getChangelogThread()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {

                    ChangeLogWindow changeLogWindow = new ChangeLogWindow(webClient.DownloadString(changelogPasteBin));
                    changeLogWindow.ShowDialog();
                }
            }
            catch(Exception ex)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    logBoxText.Text += $"Couldn't recieve Changelog ({ex.GetType().Name})";
                    logBoxText.Text += Environment.NewLine;
                }));
            }
        }

        private void sendDataToArduino(LedItem ledItem, string mode, bool setLedItemClassArgs)
        {
            try
            {
                ledItem.InitSerial();

                //sets the overlap mode if mode is music
                if (ledItem.CurrentMode == "Music")
                {
                    mode = ledItem.OverlappedMusicMode;
                    ledItem.IsMusic = 1;
                }
                else
                {
                    ledItem.IsMusic = 0;
                }

                //get the selected mode shortname and get the arguments
                string tmpMode = null;

                byte arg1 = 0;
                byte arg2 = 0;
                byte arg3 = 0;

                byte arg4 = 0;
                byte arg5 = 0;
                byte arg6 = 0;

                byte arg7 = 0;
                byte arg8 = 0;
                byte arg9 = 0;

                switch (mode)
                {
                    case "Static":
                        tmpMode = "STTC";

                        //set the arguments for the usb message
                        arg1 = modeStatic.RealColor.R;
                        arg2 = modeStatic.RealColor.G;
                        arg3 = modeStatic.RealColor.B;

                        //set the arguments in the leditem class
                        if (setLedItemClassArgs)
                        {
                            ledItem.StaticModeColor = modeStatic.SelectedColor;
                            ledItem.StaticBrightness = modeStatic.Brightness;
                        }

                        break;

                    case "Cycle":
                        tmpMode = "CYCL";

                        //set the arguments for the usb message
                        arg7 = modeCycle.Speed;
                        arg9 = modeCycle.Brightness;

                        //set the arguments in the leditem class
                        if (setLedItemClassArgs)
                        {
                            ledItem.CycleSpeed = modeCycle.Speed;
                            ledItem.CycleBrightness = modeCycle.Brightness;
                        }

                        break;

                    case "Rainbow":
                        tmpMode = "RNBW";

                        //set the arguments for the usb message
                        arg7 = modeRainbow.Speed;
                        arg9 = modeRainbow.Brightness;

                        //set the arguments in the leditem class
                        if (setLedItemClassArgs)
                        {
                            ledItem.RainbowSpeed = modeRainbow.Speed;
                            ledItem.RainbowBrightness = modeRainbow.Brightness;
                        }

                        break;

                    case "Lightning":
                        tmpMode = "LING";

                        //set the arguments for the usb message (can't just use the realcolor property here because of a invalidopertationexception I can't fix)
                        arg1 = (byte)(modeLightning.SelectedColor.R * (float)(modeLightning.Brightness / 255));
                        arg2 = (byte)(modeLightning.SelectedColor.G * (float)(modeLightning.Brightness / 255));
                        arg3 = (byte)(modeLightning.SelectedColor.B * (float)(modeLightning.Brightness / 255));

                        //set the arguments in the leditem class
                        if (setLedItemClassArgs)
                        {
                            ledItem.LightningModeColor = modeLightning.SelectedColor;
                            ledItem.LightningBrightness = modeLightning.Brightness;
                        }

                        break;

                    case "Color Overlay":
                        tmpMode = "OVRL";

                        //set the arguments for the usb message
                        arg7 = modeOverlay.Speed;
                        arg8 = modeOverlay.Direction;

                        //set the arguments in the leditem class
                        if (setLedItemClassArgs)
                        {
                            ledItem.OverlaySpeed = modeOverlay.Speed;
                            ledItem.OverlayDirection = modeOverlay.Direction;
                        }

                        break;

                    case "Color Spinner":
                        tmpMode = "SPIN";

                        //set the arguments for the usb message
                        arg1 = (byte)((float)modeSpinner.SpinnerColor.R * (float)((float)modeSpinner.SpinnerColorBrightness / 255.00));
                        arg2 = (byte)((float)modeSpinner.SpinnerColor.G * (float)((float)modeSpinner.SpinnerColorBrightness / 255.00));
                        arg3 = (byte)((float)modeSpinner.SpinnerColor.B * (float)((float)modeSpinner.SpinnerColorBrightness / 255.00));

                        arg4 = (byte)((float)modeSpinner.BackgroundColor.R * (float)((float)modeSpinner.BackgroundColorBrightness / 255.00));
                        arg5 = (byte)((float)modeSpinner.BackgroundColor.G * (float)((float)modeSpinner.BackgroundColorBrightness / 255.00));
                        arg6 = (byte)((float)modeSpinner.BackgroundColor.B * (float)((float)modeSpinner.BackgroundColorBrightness / 255.00));

                        arg7 = modeSpinner.Speed;
                        arg9 = modeSpinner.Length;

                        //set the arguments in the leditem class
                        if (setLedItemClassArgs)
                        {
                            ledItem.SpinnerModeSpinnerColor = modeSpinner.SpinnerColor;
                            ledItem.SpinnerModeBackgroundColor = modeSpinner.BackgroundColor;

                            ledItem.SpinnerModeSpinnerColorBrightness = modeSpinner.SpinnerColorBrightness;
                            ledItem.SpinnerModeBackgroundColorBrightness = modeSpinner.BackgroundColorBrightness;

                            ledItem.SpinnerSpeed = modeSpinner.Speed;
                            ledItem.SpinnerLength = modeSpinner.Length;
                        }

                        break;
                }

                //change the mode to off
                if (!ledItem.IsOn)
                {
                    tmpMode = "TOFF";
                }

                string pins = null;

                //get the pins
                if (ledItem.Type == 0)
                {
                    pins = ledItem.DPIN.ToString("D2") + ledItem.LedCount.ToString("D4");
                }
                else if (ledItem.Type == 1)
                {
                    pins = ledItem.RPIN.ToString("D2") + ledItem.GPIN.ToString("D2") + ledItem.BPIN.ToString("D2");
                }

                //generate the usb message:     #MODE(4)ISMUSIC(1)TYPE(1)PINS(6)COLOR/ARGS(3 x 3)ID(2)\\n
                string message = $"#{tmpMode}{ledItem.IsMusic}{ledItem.Type}{pins}{arg1.ToString("D3")}{arg2.ToString("D3")}{arg3.ToString("D3")}{arg4.ToString("D3")}{arg5.ToString("D3")}{arg6.ToString("D3")}{arg7.ToString("D3")}{arg8.ToString("D3")}{arg9.ToString("D3")}{ledItem.ID.ToString("D2")}\\n";

                ledItem.SerialWrite(message);

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    logBoxText.Text += $"Send: {message} using BAUD-{ledItem.BaudRate} on {ledItem.ComPortName}";
                    logBoxText.Text += Environment.NewLine;
                }));
            }
            catch (Exception ex)
            {
                AlertWindow alert = new AlertWindow($"Can't write to {ledItem.ComPortName}", false);
                alert.ShowDialog();

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    logBoxText.Text += $"Can't write to {ledItem.ComPortName} ({ ex.GetType().Name })";
                    logBoxText.Text += Environment.NewLine;
                }));
            }
        }

        private void getArduinoInformation(Arduino arduino)
        {
            //get information from the arduino using by sending a _ command
            try
            {
                if (arduino.ActiveSerialPort.IsOpen)
                    arduino.ActiveSerialPort.Write("_\\n");
            }
            catch (Exception ex)
            {
                logBoxText.Text += $"Couldn't check for Version on { arduino.SerialPortName } ({ ex.GetType().Name })";
                logBoxText.Text += Environment.NewLine;
            }
        }

        //events
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            saveProgram();

            foreach (Arduino c in Arduino.AllArduinos)
            {
                try
                {
                    c.ActiveSerialPort.Close();
                }
                catch { }
            }

            Environment.Exit(0);
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings = true;
        }

        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            MadeChanges = false;

            //set the correct mode for the selectedLedItem
            SelectedLedItem.CurrentMode = SelectedMode;

            if (SelectedLedItem.CurrentMode == "Music")
            {
                SelectedLedItem.MusicFrequency = (int)modeMusic.freqSlider.Value;
                logBoxText.Text += $"Set Active Audio Device to {modeMusic.SelectedDevice?[1]}";
                logBoxText.Text += Environment.NewLine;
            }

            //check if any device is using the music mode and if so, start the analyzer
            bool usingMusic = false;

            foreach (LedItem item in LedItem.AllItems)
            {
                if (item.CurrentMode == "Music")
                {
                    //init bass.dll
                    if (modeMusic.SoundAnalyzer == null)
                        modeMusic.InitAnalyzer();

                    usingMusic = true;
                }
            }

            new Thread(new ThreadStart(() =>
            {
                if (modeMusic.SoundAnalyzer != null)
                    modeMusic.SoundAnalyzer.Enable = false;

                //send the data to the arduino
                sendDataToArduino(SelectedLedItem, SelectedMode, true);

                this.Dispatcher.Invoke(saveProgram);

                if (modeMusic.SoundAnalyzer != null)
                {
                    //set a small delay before starting the soundanalyzer
                    modeMusic.SoundAnalyzer.Enable = usingMusic;
                }
            }))
            { IsBackground = true, ApartmentState = ApartmentState.STA }.Start();
        }

        private void ClosingStoryboard_Completed(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            isMinimized = true;
            saveProgram();
        }

        private void MinimizeAppBtn_Click(object sender, RoutedEventArgs e)
        {
            lastTop = this.Top;

            DoubleAnimation minimAnimO = new DoubleAnimation()
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                FillBehavior = FillBehavior.Stop
            };

            DoubleAnimationUsingKeyFrames minimAnimT = new DoubleAnimationUsingKeyFrames()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                FillBehavior = FillBehavior.Stop
            };

            minimAnimT.KeyFrames.Add(new SplineDoubleKeyFrame(
                this.Top + 150,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
                new KeySpline(0.6, 0.0, 0.9, 0.0)
                )
            );

            minimAnimO.Completed += MinimAnim_Completed;
            this.BeginAnimation(Window.OpacityProperty, minimAnimO);
            this.BeginAnimation(Window.TopProperty, minimAnimT);
        }

        private void MaximizeAppBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
        }

        private void RestoreAppBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void CloseAppBtn_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation closeAnim = new DoubleAnimation()
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                FillBehavior = FillBehavior.Stop
            };

            closeAnim.Completed += ClosingStoryboard_Completed;
            this.BeginAnimation(Window.OpacityProperty, closeAnim);
        }

        private void MinimAnim_Completed(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            this.Top = lastTop;
        }

        private void AddItemBtn_Click(object sender, RoutedEventArgs e)
        {
            LedItem item = new LedItem();
        }

        private void ModeTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.Source == modeTabControl)
            {
                TabItem selectedItem = (TabItem)modeTabControl.SelectedItem;

                if (SelectedMode != selectedItem.Header.ToString() && ShowMainGrid)
                    SelectedMode = selectedItem.Header.ToString();
            }
        }

        private void SysTrayOpenBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.Opacity = 1;
            isMinimized = false;
        }

        private void SysTrayCloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ItemOnBtn_ToggledChanged(object sender, EventArgs e)
        {
            sendDataToArduino(SelectedLedItem, SelectedMode, true);

            saveProgram();
        }

        private void UploadArduinoBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowArduinoUploadWindow = true;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                restoreAppBtn.Visibility = Visibility.Visible;
                maximizeAppBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                maximizeAppBtn.Visibility = Visibility.Visible;
                restoreAppBtn.Visibility = Visibility.Collapsed;
            }

            isMinimized = this.WindowState == WindowState.Minimized;
        }

        private void Icon_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            iconPressed++;

            if(iconPressed == 8)
            {
                devTB.FontSize = 1;

                Storyboard sbI = this.FindResource("easterEggStoryboardIcon") as Storyboard;
                Storyboard.SetTarget(sbI, icon);
                sbI.Completed += (s, ev) => 
                {
                    IsDev = true;

                    Storyboard sbD = this.FindResource("easterEggStoryboardTB") as Storyboard;
                    Storyboard.SetTarget(sbD, devTB);
                    
                    sbD.Completed += (o, eve) => 
                    {
                        devTB.FontSize = 10;
                    };
                    sbD.Begin();
                };
                sbI.Begin();
            }
        }

        //getters and setters
        public static int IDCounter
        {
            get { return idCounter; }
            set { idCounter = value; }
        }

        public static string ArduinoBinaryPasteBin
        {
            get { return arduinoBinaryPasteBin; }
        }

        public static string CurrentVersion
        {
            get { return currentVersion; }
        }

        public static string ApplicationName
        {
            get { return appName; }
        }

        public static MainWindow ActiveWindow
        {
            get { return activeWindow; }
        }

        public byte LoadingProgress
        {
            get { return loadingProgress; }
            set
            {
                loadingProgress = value;
                OnPropertyChanged("LoadingProgress");
            }
        }

        public bool IsDev
        {
            get { return isDev; }
            set
            {
                isDev = value;
                
                if(value)
                {
                    icon.Visibility = Visibility.Collapsed;
                    devTB.Visibility = Visibility.Visible;
                }
                else
                {
                    icon.Visibility = Visibility.Visible;
                    devTB.Visibility = Visibility.Collapsed;
                }

                OnPropertyChanged("IsDev");
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

        public bool StartBassNet
        {
            get { return startBassNet; }
            set
            {
                startBassNet = value;
                OnPropertyChanged("StartBassNet");
            }
        }

        public bool AutoUpdate
        {
            get { return autoUpdate; }
            set
            {
                autoUpdate = value;
                OnPropertyChanged("AutoUpdate");
            }
        }

        public bool EnableFanAnim
        {
            get { return enableFanAnim; }
            set
            {
                enableFanAnim = value;
                OnPropertyChanged("EnableFanAnim");
            }
        }

        public bool EnableLogBox
        {
            get { return logBox.IsVisible; }
            set
            {
                if (!logBox.IsVisible && value == true)
                {
                    logBox.Visibility = Visibility.Visible;
                    this.Height = this.Height + logBox.Height;
                }
                else if (logBox.IsVisible && value == false)
                {
                    logBox.Visibility = Visibility.Collapsed;
                    this.Height = this.Height - logBox.Height;
                }
            }
        }

        public bool BlockPopups
        {
            get { return blockPopups; }
            set
            {
                blockPopups = value;
                OnPropertyChanged("BlockPopups");
            }
        }

        public bool IsMinimized
        {
            get { return isMinimized; }
        }

        public bool ShowProperties
        {
            get { return showProperties; }
            set
            {
                showProperties = value;
                OnPropertyChanged("ShowProperties");

                //save the program when the properties window is closed
                if (!value)
                    saveProgram();
            }
        }

        public bool ShowSettings
        {
            get { return showSettings; }
            set
            {
                showSettings = value;

                if (value)
                {
                    SettingsWindow = new Settings();
                    settingsViewbox.Child = SettingsWindow;
                }
                else
                {
                    saveProgram();
                }

                OnPropertyChanged("ShowSettings");
            }
        }

        public bool ShowMainGrid
        {
            get { return showMainGrid; }
            set
            {
                showMainGrid = value;
                OnPropertyChanged("ShowMainGrid");
            }
        }

        public bool ShowColorPicker
        {
            get { return showColorPicker; }
            set
            {
                showColorPicker = value;
                OnPropertyChanged("ShowColorPicker");
            }
        }

        public bool ShowArduinoUploadWindow
        {
            get { return showArduinoUploadWindow; }
            set
            {
                showArduinoUploadWindow = value;
                OnPropertyChanged("ShowArduinoUploadWindow");

                //check if connection can be established
                new Thread(new ThreadStart(() =>
                {
                    if (!Update.CheckForInternet() && value)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            new AlertWindow("Can't connect to the internet!").ShowDialog();

                            ShowArduinoUploadWindow = false;

                            logBoxText.Text += "Can't connect to the internet (Unable to connect to 'http://google.com/generate_204')";
                            logBoxText.Text += Environment.NewLine;
                        }));
                    }

                }))
                { ApartmentState = ApartmentState.STA, IsBackground = true }.Start();
            }
        }

        public string SelectedMode
        {
            get { return selectedMode; }
            set
            {
                selectedMode = value;

                //selects the correct tabitem
                foreach (TabItem tabItem in modeTabControl.Items)
                {
                    if (tabItem.Header.ToString() == value)
                    {
                        modeTabControl.SelectedItem = tabItem;
                    }
                }

                MadeChanges = true;
            }
        }

        public string AppDataApplicationDirectory
        {
            get
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ApplicationName + @"\");

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                return directory;
            }
        }

        public string ArduinoBinaryDirectory
        {
            get
            {
                string directory = Path.Combine(Path.GetTempPath(), $@"{ApplicationName}ArduinoBinary\");

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                return directory;
            }
        }

        public string UserDirectory
        {
            get
            {
                string directory = Path.Combine(AppDataApplicationDirectory,@"user\");

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                return directory;
            }
        }

        public string SettingsFileFullPath
        {
            get
            {
                string filePath = UserDirectory + @"settings.xml";

                if (!File.Exists(filePath))
                {
                    using (var file = File.Create(filePath))
                    {
                    }

                    //write something to the xml (it won't be readable if its empty)
                    XmlTextWriter xml = new XmlTextWriter(filePath, Encoding.UTF8);
                    xml.Formatting = Formatting.Indented;
                    xml.WriteStartElement("Settings");
                    xml.WriteEndElement();
                    xml.Close();
                }
                else
                {
                    //try to load the xml file. if the file cant be loaded, create a new file
                    try
                    {
                        XmlDocument xmlDocument = new XmlDocument();

                        xmlDocument.Load(filePath);
                    }
                    catch (XmlException)
                    {
                        XmlTextWriter xml = new XmlTextWriter(filePath, Encoding.UTF8);
                        xml.Formatting = Formatting.Indented;
                        xml.WriteStartElement("Settings");
                        xml.WriteEndElement();
                        xml.Close();
                    }
                }

                return filePath;
            }
        }

        public LedItem SelectedLedItem
        {
            get { return selectedItem; }
            set
            {
                if (value == null)
                {
                    noItemsAlert.Visibility = Visibility.Visible;
                }
                else
                {
                    //reset the background of the old selected item and set the new item background color
                    if (selectedItem != null)
                        selectedItem.Background = new SolidColorBrush(Colors.Transparent);

                    selectedItem = value;
                    SelectedLedItem.SetResourceReference(Control.BackgroundProperty, "AccentColor");
                    OnPropertyChanged("SelectedLedItem");

                    //set the correct mode
                    SelectedMode = selectedItem.CurrentMode;
                    noItemsAlert.Visibility = Visibility.Collapsed;

                    //set the overlap mode for music
                    modeMusic.OverlappedMode = SelectedLedItem.OverlappedMusicMode;

                    //set every mode argument for the selected leditem
                    modeStatic.Brightness = SelectedLedItem.StaticBrightness;
                    modeStatic.SelectedColor = SelectedLedItem.StaticModeColor;
                    modeCycle.Speed = SelectedLedItem.CycleSpeed;
                    modeCycle.Brightness = SelectedLedItem.CycleBrightness;
                    modeRainbow.Brightness = SelectedLedItem.RainbowBrightness;
                    modeRainbow.Speed = SelectedLedItem.RainbowSpeed;
                    modeOverlay.Speed = SelectedLedItem.OverlaySpeed;
                    modeOverlay.Direction = SelectedLedItem.OverlayDirection;
                    modeLightning.SelectedColor = SelectedLedItem.LightningModeColor;
                    modeLightning.Brightness = SelectedLedItem.LightningBrightness;
                    modeSpinner.SpinnerColor = SelectedLedItem.SpinnerModeSpinnerColor;
                    modeSpinner.BackgroundColor = SelectedLedItem.SpinnerModeBackgroundColor;
                    modeSpinner.BackgroundColorBrightness = SelectedLedItem.SpinnerModeBackgroundColorBrightness;
                    modeSpinner.SpinnerColorBrightness = SelectedLedItem.SpinnerModeSpinnerColorBrightness;
                    modeSpinner.Speed = SelectedLedItem.SpinnerSpeed;
                    modeSpinner.Length = SelectedLedItem.SpinnerLength;
                }
            }
        }

        public Color AccentColor
        {
            get { return accentColor; }
            set
            {
                accentColor = value;
                OnPropertyChanged("AccentColor");
            }
        }

        public Settings SettingsWindow
        {
            get { return settingsWindow; }
            set { settingsWindow = value; }
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