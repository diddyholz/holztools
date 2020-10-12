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
using System.Net.Sockets;
using System.Net.Http;
using System.Net.Mail;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Windows.Data;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.PropertyGrid;
using System.Net.NetworkInformation;
using System.Linq;
using System.Diagnostics.Contracts;

namespace HolzTools
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        public static MainWindow activeWindow = null;

        public static string InstallLocation = "";

        private static int idCounter = 0;

        private const string currentVersion = "1.08";
        private const string updatePasteBin = "https://pastebin.com/raw/t2r0pWMr";
        private const string changelogPasteBin = "https://pastebin.com/raw/mQK7VVGZ";
        private const string arduinoBinaryPasteBin = "https://pastebin.com/raw/eAYERLEs";
        private const string arduinoBinaryChangelogPasteBin = "https://pastebin.com/raw/VHdBVP3b";
        private const string appName = "HolzTools";

        private bool isDev = false;
        private bool showProperties = false;
        private bool showSettings = false;
        private bool showMainGrid = false;
        private bool showColorPicker = false;
        private bool showArduinoUploadWindow = false;
        private bool showMultiColorEditorWindow = false;
        private bool autoUpdate = true;
        private bool madeChanges = false;
        private bool blockPopups = false;
        private bool startBassNet = true;
        private bool isMinimized = false;
        private bool enableFanAnim = false;
        private bool syncAvailable = true;
        private bool firstLoad = true;
        private bool enableLogBox = false;
        private bool showNotification = false;

        private int tcpPort = 39769;
        private int tcpTimeout = 200;

        private double lastTop = 0.00;

        private byte loadingProgress = 0;
        private byte iconPressed = 0;

        private string selectedMode = "Static";
        private string notificationText = "";

        private List<string> notificationList = new List<string>();

        private Color accentColor = Color.FromRgb(200, 0, 0);

        private LedItem selectedItem = null;

        private Settings settingsWindow;

        private ArduinoBinaryDownloaderWindow arduinoBinaryDownloadWindow;

        private Thread tcpListenerThread = null;

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

        public static List<string> GetNetworkDevices()
        {
            try
            {
                List<string> discoveredIps = new List<string>();
                List<string> discoveredESP32 = new List<string>();

                UdpClient u = new UdpClient("8.8.8.8", 80);
                IPAddress localAddr = ((IPEndPoint)u.Client.LocalEndPoint).Address;
                u.Close();

                string prefix = localAddr.ToString().Substring(0, localAddr.ToString().LastIndexOf('.') + 1);

                int completedPings = 0;

                // get all ips by pinging them
                for (int x = 0; x < 256; x++)
                {
                    Ping ping = new Ping();

                    ping.PingCompleted += (s, e) =>
                    {
                        string ip = (string)e.UserState.ToString(); ;

                        MainWindow.ActiveWindow.Dispatcher.Invoke(() =>
                        {
                            completedPings++;
                        });

                        if (e.Reply != null && e.Reply.Status == IPStatus.Success)
                        {
                            discoveredIps.Add(ip);
                        }
                    };

                    ping.SendAsync(prefix + x.ToString(), 1000, prefix + x.ToString());
                }

                while (completedPings != 256)
                    Thread.Sleep(100);

                discoveredIps.Sort();

                List<HTTPAttribute> attrs = new List<HTTPAttribute>();
                attrs.Add(new HTTPAttribute("Command", TCPGETINFO));


                // check if these ips are ESP32 by sending a GETINFO request
                foreach (string ip in discoveredIps)
                {
                    string res = SendGetRequest(ip, 39769, attrs);

                    if (res != TCPCOULDNOTCONNECT.ToString() && res.IndexOf("Hostname=") != -1)
                    {
                        string[] args = res.Split('&');

                        foreach(string s in args)
                        {
                            string argName = s.Split('=')[0];
                            string argValue = s.Split('=')[1];

                            if(argName == "Hostname")
                                discoveredESP32.Add($"{argValue} ({ip})");
                        }
                        
                    }
                }

                return discoveredESP32.Distinct().ToList();
            }
            catch
            {
                return new List<string>();
            }

        }

        public static string SendGetRequest(string url, int port, List<HTTPAttribute> attributes, int timeout = 200)
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = "http://" + url;

            url += $":{port}?";

            foreach (HTTPAttribute attr in attributes)
            {
                url += $"{attr.AttributeName}={attr.AttributeValue}&";
            }

            url = url.TrimEnd('&');

            try
            {

                string urlData = "";
                WebRequest wr = WebRequest.Create(new Uri(url));
                wr.Timeout = timeout;
                urlData = new StreamReader(wr.GetResponse().GetResponseStream()).ReadToEnd();

                return urlData;
            }
            catch(Exception e)
            {
                return TCPCOULDNOTCONNECT.ToString();
            }
        }

        public MainWindow()
        {
            //get and set the installLocation of the program
            InstallLocation = Assembly.GetEntryAssembly().Location;

            activeWindow = this;

            InitializeComponent();

            DataContext = this;

            SourceInitialized += (s, e) =>
            {
                IntPtr handle = (new WindowInteropHelper(this)).Handle;
                HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WindowProc));
            };

            // events
            logBoxText.TextChanged += (o, e) => { logBoxText.ScrollToEnd(); };

            Thread applicationStartThread = new Thread(() => applicationStart());
            applicationStartThread.IsBackground = true;
            applicationStartThread.SetApartmentState(ApartmentState.STA);
            applicationStartThread.Start();
        }

        public void CheckForUpdate(bool notifyIfNoUpdate = false)
        {
            UpdateWindow updateWindow = new UpdateWindow();

            Update update = new Update(currentVersion, updatePasteBin, Process.GetCurrentProcess().Id, updateWindow.updateProgressBar, updateWindow.updateTextBlock, updateWindow);

            bool updateAvailable = false;

            //check for internet connection
            if (!Update.CheckForInternet())
            {
                PutNotification("Could not check for updates! (No internet connection available)");

                // starts a thread that keeps looking for an internet connection in the background
                new Thread(() =>
                {
                    while(!Update.CheckForInternet())
                    {
                        Thread.Sleep(10000);
                    }

                    if(notificationList.Count > 1 && notificationList.Contains("Could not check for updates! (No internet connection available)"))
                    {
                        notificationList.Remove("Could not check for updates! (No internet connection available)");
                        NotificationText = notificationList[0];
                    }
                    else if(notificationList.Count == 1 && notificationList.Contains("Could not check for updates! (No internet connection available)"))
                    {
                        notificationList.Clear();
                        ShowNotification = false;
                    }

                    CheckForUpdate();
                })
                { IsBackground = true }.Start();

                this.Dispatcher.Invoke(new Action(() =>
                {
                    logBoxText.Text += $"Couldn't check for updates (Unable to connect to 'http://google.com/generate_204')";
                    logBoxText.Text += Environment.NewLine;
                }));

                return;
            }

            //check for application update
            if (update.CheckForUpdate())
            {
                updateAvailable = true;

                string changelogString = "";

                using(WebClient wc = new WebClient())
                {
                    changelogString = wc.DownloadString(changelogPasteBin);
                }

                NewUpdateWindow updateAlert = new NewUpdateWindow(update.LatestVersion, changelogString);
                updateAlert.ShowDialog();

                if (updateAlert.DialogResult.Value)
                {
                    update.Upgrade();
                }
            }

            //check for arduino and esp binary update
            using (WebClient wc = new WebClient())
            {
                string newestVersion = wc.DownloadString(arduinoBinaryPasteBin).Split('|')[0];

                List<Arduino> updatableArduinos = new List<Arduino>();

                //check which arduinos can be updated
                foreach (Arduino arduino in Arduino.AllArduinos)
                {
                    // retry 3 times
                    byte tryCount = 0;

                    while(tryCount < 3)
                    {
                        getArduinoInformation(arduino);

                        var currentTime = DateTime.Now;

                        while(DateTime.Now.Subtract(currentTime).TotalMilliseconds < 1000 && arduino.BinaryVersion == "")
                        {
                            Thread.Sleep(100);
                        }

                        if (arduino.BinaryVersion == "")
                            tryCount++;
                        else
                            tryCount = 3;
                    }

                    if (arduino.BinaryVersion == "")
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            PutNotification($"Could not receive the binary information of your Arduino ({arduino.SerialPortName})! Please check its connection.");
                        });
                    }
                    else if (arduino.BinaryVersion != newestVersion)
                    {
                        updatableArduinos.Add(arduino);
                    }
                }

                if (updatableArduinos.Count > 0)
                {
                    updateAvailable = true;

                    NewUpdateWindow alert = new NewUpdateWindow(newestVersion, wc.DownloadString(arduinoBinaryChangelogPasteBin), true, (byte)updatableArduinos.Count);

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
                                    arduinoBinaryDownloadWindow.updateProgressBar.Value = 70;
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
                                    logBoxText.Text += $"Finished flashing of binary of model { arduino.ArduinoType } at { arduino.SerialPortName }";
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

                        //set every led mode again
                        setEveryLedMode();
                    }
                }

                List<string> updatableESP32s = new List<string>();

                //check which ESP32 can be updated
                foreach (LedItem item in LedItem.AllItems.Where(x => x.IsNetwork))
                {
                    if (updatableESP32s.Contains(item.IpAddress))
                        continue;

                    List<HTTPAttribute> attrs = new List<HTTPAttribute>();
                    attrs.Add(new HTTPAttribute("Command", TCPGETINFO));

                    string res = SendGetRequest(item.IpAddress, item.ServerPort, attrs);

                    if (res == TCPCOULDNOTCONNECT.ToString())
                    {
                        PutNotification($"Cannot connect to your ESP32 at {item.IpAddress}! Please check if it received a new IP-Address.");
                        continue;
                    }

                    if (res.Split('&').Length < 2 && !res.Contains("Version"))
                        continue;

                    foreach(string s in res.Split('&'))
                    {
                        string argName = s.Split('=')[0];

                        if(argName == "Version")
                        {
                            string espVersion = s.Split('=')[1];

                            if (espVersion != newestVersion)
                            {
                                updatableESP32s.Add(item.IpAddress);
                            }
                        }
                    }
                }

                if (updatableESP32s.Count > 0)
                {
                    NewUpdateWindow alert = new NewUpdateWindow(newestVersion, wc.DownloadString(arduinoBinaryChangelogPasteBin), true, (byte)updatableESP32s.Count);

                    alert.ShowDialog();

                    if (alert.DialogResult.Value)
                    {
                        string[] downloadString = wc.DownloadString(ArduinoBinaryPasteBin).Split('|');

                        string fileName = "";

                        //upload the binary to the ESP32s
                        foreach (string ip in updatableESP32s)
                        {
                            string downloadUrl = "";

                            bool finishedDownloading = false;

                            //set the filename for the binary
                            fileName = ArduinoBinaryDirectory + $@"binary{downloadString[0]}mESP32.hex";

                            //delete the file if it already exists
                            if (File.Exists(fileName))
                                File.Delete(fileName);

                            //set the correct downloadlink
                            downloadUrl = downloadString[2];

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
                                    arduinoBinaryDownloadWindow.updateProgressBar.Value = 70;
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
                                // logic for ESP32 OTA updater
                            }
                            catch (Exception ex)
                            {
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    new AlertWindow("Failed to upload binary!", false).ShowDialog();

                                    logBoxText.Text += $"Failed to upload binary to your ESP32 at {ip} ({ex.GetType().Name})";
                                    logBoxText.Text += Environment.NewLine;
                                }));
                            }

                            this.Dispatcher.BeginInvoke(new Action(() => arduinoBinaryDownloadWindow.Close()));
                        }
                    }
                }
            }

            if (notifyIfNoUpdate && !updateAvailable)
                new AlertWindow("No updates available").ShowDialog();
        }

        public void PutNotification(string notificationText)
        {
            if (notificationList.Contains(notificationText))
                return;

            ShowNotification = true;
            
            if(notificationList.Count == 0)
                NotificationText = notificationText;

            notificationList.Add(notificationText);
        }

        private void tcpListenerLoop()
        {
            TcpListener server = null;

            try
            {
                IPAddress localAddr = IPAddress.Parse("0.0.0.0");

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, TCPPort);

                // Start listening for client requests
                server.Start();

                // Enter the listening loop
                while (true)
                {
                    // Perform a blocking call to accept requests
                    TcpClient client = server.AcceptTcpClient();

                    string data = "";
                    string response = "";

                    // Get a stream object for reading and writing and process the request
                    using (StreamWriter sw = new StreamWriter(client.GetStream()))
                    using (StreamReader sr = new StreamReader(client.GetStream()))
                    {
                        string readData = "";

                        do
                        {
                            readData = sr.ReadLine();
                            data += (readData + Environment.NewLine);
                        } while (readData.Length > 0);

                        data = Uri.UnescapeDataString(data);

                        // decode the request
                        string[] tmp = data.Split('/');
                        tmp = tmp[1].Split('&');
                        tmp[0] = tmp[0].Substring(1);
                        tmp[tmp.Length - 1] = tmp[tmp.Length - 1].Split(' ')[0];

                        string command = "";
                        string ledItemName = "";
                        string mode = "";

                        bool isOn = true;

                        byte staticBrightness = 255;
                        byte cycleBrightness = 255;
                        byte rainbowBrightness = 255;
                        byte lightningBrightness = 255;
                        byte cycleSpeed = 0;
                        byte rainbowSpeed = 0;
                        byte overlaySpeed = 0;
                        byte overlayDirection = 0;
                        byte spinnerSpeed = 0;
                        byte spinnerLength = 0;
                        byte spinnerColorBrightness = 255;
                        byte backgroundColorBrightness = 255;

                        Color staticModeColor = new Color();
                        Color lightningModeColor = new Color();
                        Color spinnerModeSpinnerColor = new Color();
                        Color spinnerModeBackgroundColor = new Color();

                        LedItem ledItem = null;

                        // get all arguments
                        foreach (string arg in tmp)
                        {
                            string argName = arg.Split('=')[0];
                            string argValue = arg.Split('=')[1];

                            switch (argName)
                            {
                                case "Command":
                                    command = argValue;
                                    break;

                                case "LEDItem":
                                    ledItemName = argValue;
                                    break;

                                case "LEDMode":
                                    mode = argValue.Replace('_', ' ');
                                    break;

                                case "IsOn":
                                    isOn = Convert.ToBoolean(argValue);
                                    break;

                                case "StaticBrightness":
                                    staticBrightness = Convert.ToByte(argValue);
                                    break;

                                case "CycleBrightness":
                                    cycleBrightness = Convert.ToByte(argValue);
                                    break;

                                case "RainbowBrightness":
                                    rainbowBrightness = Convert.ToByte(argValue);
                                    break;

                                case "LightningBrightness":
                                    lightningBrightness = Convert.ToByte(argValue);
                                    break;

                                case "CycleSpeed":
                                    cycleSpeed = Convert.ToByte(argValue);
                                    break;

                                case "RainbowSpeed":
                                    rainbowSpeed = Convert.ToByte(argValue);
                                    break;

                                case "OverlaySpeed":
                                    overlaySpeed = Convert.ToByte(argValue);
                                    break;

                                case "OverlayDirection":
                                    overlayDirection = Convert.ToByte(argValue);
                                    break;

                                case "SpinnerSpeed":
                                    spinnerSpeed = Convert.ToByte(argValue);
                                    break;

                                case "SpinnerLength":
                                    spinnerLength = Convert.ToByte(argValue);
                                    break;

                                case "SpinnerColorBrightness":
                                    spinnerColorBrightness = Convert.ToByte(argValue);
                                    break;

                                case "BackgroundColorBrightness":
                                    backgroundColorBrightness = Convert.ToByte(argValue);
                                    break;

                                case "StaticModeColor":
                                    staticModeColor = Color.FromRgb(Convert.ToByte(argValue.Split(',')[0]), Convert.ToByte(argValue.Split(',')[1]), Convert.ToByte(argValue.Split(',')[2]));
                                    break;

                                case "LightningModeColor":
                                    lightningModeColor = Color.FromRgb(Convert.ToByte(argValue.Split(',')[0]), Convert.ToByte(argValue.Split(',')[1]), Convert.ToByte(argValue.Split(',')[2]));
                                    break;

                                case "SpinnerModeSpinnerColor":
                                    spinnerModeSpinnerColor = Color.FromRgb(Convert.ToByte(argValue.Split(',')[0]), Convert.ToByte(argValue.Split(',')[1]), Convert.ToByte(argValue.Split(',')[2]));
                                    break;

                                case "SpinnerModeBackgroundColor":
                                    spinnerModeBackgroundColor = Color.FromRgb(Convert.ToByte(argValue.Split(',')[0]), Convert.ToByte(argValue.Split(',')[1]), Convert.ToByte(argValue.Split(',')[2]));
                                    break;
                            }
                        }

                        if (command == TCPSETLED)
                        {
                            // find the correct led
                            foreach (LedItem item in LedItem.AllItems)
                            {
                                if (item.ItemName == ledItemName)
                                    ledItem = item;
                            }

                            if (ledItem != null)
                            {
                                // set the mode and mode preferences
                                ledItem.CurrentMode = mode;
                                ledItem.IsOn = isOn;

                                switch (mode)
                                {
                                    case "Static":
                                        ledItem.StaticModeColor = staticModeColor;
                                        ledItem.StaticBrightness = staticBrightness;
                                        break;

                                    case "Cycle":
                                        ledItem.CycleBrightness = cycleBrightness;
                                        ledItem.CycleSpeed = cycleSpeed;
                                        break;

                                    case "Rainbow":
                                        ledItem.RainbowBrightness = rainbowBrightness;
                                        ledItem.RainbowSpeed = rainbowSpeed;
                                        break;

                                    case "Lightning":
                                        ledItem.LightningModeColor = lightningModeColor;
                                        ledItem.LightningBrightness = lightningBrightness;
                                        break;

                                    case "Color Overlay":
                                        ledItem.OverlaySpeed = overlaySpeed;
                                        ledItem.OverlayDirection = overlayDirection;
                                        break;

                                    case "Color Spinner":
                                        ledItem.SpinnerModeSpinnerColor = spinnerModeSpinnerColor;
                                        ledItem.SpinnerModeSpinnerColorBrightness = spinnerColorBrightness;
                                        ledItem.SpinnerModeBackgroundColor = spinnerModeBackgroundColor;
                                        ledItem.SpinnerModeBackgroundColorBrightness = backgroundColorBrightness;
                                        ledItem.SpinnerSpeed = spinnerSpeed;
                                        ledItem.SpinnerLength = spinnerLength;
                                        break;
                                }

                                //set every mode argument for the current leditem
                                this.Dispatcher.Invoke(() =>
                                {
                                    modeStatic.Brightness = ledItem.StaticBrightness;
                                    modeStatic.SelectedColor = ledItem.StaticModeColor;
                                    modeCycle.Brightness = ledItem.CycleBrightness;
                                    modeCycle.Speed = ledItem.CycleSpeed;
                                    modeRainbow.Speed = ledItem.RainbowSpeed;
                                    modeRainbow.Brightness = ledItem.RainbowBrightness;
                                    modeLightning.Brightness = ledItem.LightningBrightness;
                                    modeLightning.SelectedColor = ledItem.LightningModeColor;
                                    modeOverlay.Speed = ledItem.OverlaySpeed;
                                    modeOverlay.Direction = ledItem.OverlayDirection;
                                    modeSpinner.Speed = ledItem.SpinnerSpeed;
                                    modeSpinner.SpinnerColor = ledItem.SpinnerModeSpinnerColor;
                                    modeSpinner.BackgroundColor = ledItem.SpinnerModeBackgroundColor;
                                    modeSpinner.Length = ledItem.SpinnerLength;
                                    modeSpinner.SpinnerColorBrightness = ledItem.SpinnerModeSpinnerColorBrightness;
                                    modeSpinner.BackgroundColorBrightness = ledItem.SpinnerModeBackgroundColorBrightness;
                                    modeSync.SelectedItemSyncableItems = ledItem.SyncableItems;
                                    modeSync.SyncedLedItem = ledItem.SyncedLedItem;
                                });

                                sendDataToArduino(ledItem, ledItem.CurrentMode, false);

                                // reset the mode arguments for the selected LedItem
                                this.Dispatcher.Invoke(() => SelectedLedItem = SelectedLedItem);

                                response = TCPREQUESTOK.ToString();
                            }
                            else
                            {
                                response = TCPLEDNOTFOUND.ToString();
                            }
                        }
                        else if (command == TCPGETINFO)
                        {
                            response = $"Hostname={Environment.MachineName}&Leds=";

                            // add every ledname
                            foreach(LedItem item in LedItem.AllItems)
                            {
                                response += $"{item.ItemName},";
                            }

                            // cut the last comma
                            response = response.Substring(0, response.Length - 1);

                            // send an error if there are no connected leds
                            if (LedItem.AllItems.Count == 0)
                                response = TCPNOCONNECTEDLEDS.ToString();
                        }
                        else
                        {
                            response = TCPINVALIDCOMMAND.ToString();
                        }

                        tcpSendResponse(response, sw);
                    }

                    // Shutdown and end connection
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                PutNotification($"Could not start the server on port {TCPPort}. The port may already be in use by another application. Try to change the port in the settings menu.");
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }
        }

        private void tcpSendResponse(string body, StreamWriter writer)
        {
            writer.Write("HTTP/1.1 200 OK");
            writer.Write(Environment.NewLine);
            writer.Write("Content-Type: text/plain; charset=UTF-8");
            writer.Write(Environment.NewLine);
            writer.Write("Content-Length: " + body.Length);
            writer.Write(Environment.NewLine);
            writer.Write(Environment.NewLine);
            writer.Write(body);
            writer.Flush();
        }

        private void applicationStart()
        {
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
            }

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                loadingText.Text = "Starting connected LEDs";
                LoadingProgress = 20;
            }));

            //set the mode of every leditem
            setEveryLedMode();

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

            this.Dispatcher.Invoke(new Action(() =>
            {
                loadingText.Text = "Finishing";
                LoadingProgress = 100;

                // create the transition animation to the mainwindow
                Style style = new Style(typeof(Grid));
                style.Setters.Add(new Setter(Grid.OpacityProperty, 1.00));

                DataTrigger splashTrigger = new DataTrigger();
                splashTrigger.Binding = new Binding("ShowMainGrid");
                splashTrigger.Value = true;

                Storyboard storyboard = new Storyboard();

                DoubleAnimationUsingKeyFrames widthAnimation = new DoubleAnimationUsingKeyFrames();
                DoubleAnimationUsingKeyFrames heightAnimation = new DoubleAnimationUsingKeyFrames();
                DoubleAnimationUsingKeyFrames opacityAnimation = new DoubleAnimationUsingKeyFrames();

                Storyboard.SetTargetProperty(widthAnimation, new PropertyPath("(Grid.Width)"));
                widthAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
                widthAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(splashGrid.Width, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
                widthAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(splashGrid.Width - 50, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
                widthAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(Application.Current.MainWindow.Width, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)), new KeySpline(0, 1, .5, 1)));

                Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("(Grid.Height)"));
                heightAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
                heightAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(splashGrid.Height, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
                heightAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(splashGrid.Height - 50, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
                heightAnimation.KeyFrames.Add(new SplineDoubleKeyFrame(Application.Current.MainWindow.Height, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)), new KeySpline(0, 1, .5, 1)));

                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("(Grid.Opacity)"));
                opacityAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(800));
                opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
                opacityAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(800))));

                storyboard.Children.Add(widthAnimation);
                storyboard.Children.Add(heightAnimation);
                storyboard.Children.Add(opacityAnimation);

                BeginStoryboard beginStoryboard = new BeginStoryboard();
                beginStoryboard.Storyboard = storyboard;

                splashTrigger.EnterActions.Add(beginStoryboard);
                splashTrigger.Setters.Add(new Setter(Grid.WidthProperty, Application.Current.MainWindow.Width));
                splashTrigger.Setters.Add(new Setter(Grid.HeightProperty, Application.Current.MainWindow.Height));
                splashTrigger.Setters.Add(new Setter(Grid.OpacityProperty, 0.00));

                style.Triggers.Add(splashTrigger);

                splashGrid.Style = style;

                SelectedLedItem = SelectedLedItem;
                ShowMainGrid = true;
                MadeChanges = false;
            }));

            //create the Tcplistener thread
            tcpListenerThread = new Thread(() => tcpListenerLoop()) { IsBackground = true };
            tcpListenerThread.SetApartmentState(ApartmentState.STA);
            tcpListenerThread.Start();

            Thread.Sleep(3000);
        }

        private void setEveryLedMode()
        {
            foreach (LedItem item in LedItem.AllItems)
            {
                if (item.IsNetwork)
                    continue;

                //send the data to the arduino (the delays have to be there because else the arduino wouldn't have enough time to start a serial connection between the pc and arduino)
                try
                {
                    item.InitSerial();
                }
                catch (Exception ex)
                {
                    if (ex.GetType() == typeof(UnauthorizedAccessException))
                        PutNotification($"Cannot write to {item.ComPortName} (The Port may already be in use)");
                    else if (ex.GetType() == typeof(NoSerialPortSelectedException))
                        PutNotification($"No COM-Port is selected for {item.ItemName}");
                    else
                        PutNotification($"Cannot write to {item.ComPortName} (The Arduino might be disconnected or connected to another port)");

                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        logBoxText.Text += $"Cannot write to {item.ComPortName} ({ ex.GetType().Name })";
                        logBoxText.Text += Environment.NewLine;
                    }));

                    continue;
                }

                Thread.Sleep(1500);

                //set every mode argument for the current leditem
                this.Dispatcher.Invoke(() =>
                {
                    modeStatic.Brightness = item.StaticBrightness;
                    modeStatic.SelectedColor = item.StaticModeColor;
                    modeStatic.Type = item.StaticType;
                    modeCycle.Brightness = item.CycleBrightness;
                    modeCycle.Speed = item.CycleSpeed;
                    modeRainbow.Speed = item.RainbowSpeed;
                    modeRainbow.Brightness = item.RainbowBrightness;
                    modeLightning.Brightness = item.LightningBrightness;
                    modeLightning.SelectedColor = item.LightningModeColor;
                    modeOverlay.Speed = item.OverlaySpeed;
                    modeOverlay.Direction = item.OverlayDirection;
                    modeSpinner.Speed = item.SpinnerSpeed;
                    modeSpinner.SpinnerColor = item.SpinnerModeSpinnerColor;
                    modeSpinner.BackgroundColor = item.SpinnerModeBackgroundColor;
                    modeSpinner.Length = item.SpinnerLength;
                    modeSpinner.SpinnerColorBrightness = item.SpinnerModeSpinnerColorBrightness;
                    modeSpinner.BackgroundColorBrightness = item.SpinnerModeBackgroundColorBrightness;
                    modeSync.SelectedItemSyncableItems = item.SyncableItems;
                    modeSync.SyncedLedItem = item.SyncedLedItem;
                });

                sendDataToArduino(item, item.CurrentMode, false);
                Thread.Sleep(400);
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

                xml.WriteStartElement("StaticType");
                xml.WriteString(item.StaticType.ToString());
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

                xml.WriteStartElement("SyncModeSyncedLedItem");
                xml.WriteString(item.SyncedLedItem);
                xml.WriteEndElement();

                xml.WriteStartElement("OverlappedMusicMode");
                xml.WriteString(item.OverlappedMusicMode);
                xml.WriteEndElement();

                xml.WriteStartElement("MusicUseExponential");
                xml.WriteString(item.MusicUseExponential.ToString());
                xml.WriteEndElement();

                //on status
                xml.WriteStartElement("On");
                xml.WriteString(item.IsOn.ToString());
                xml.WriteEndElement();

                // ledColorList for multicolor mode
                xml.WriteStartElement("LedColorList");

                foreach(Color color in item.LedColorList)
                {
                    xml.WriteStartElement("Color");

                    xml.WriteStartElement("Red");
                    xml.WriteString(color.R.ToString());
                    xml.WriteEndElement();

                    xml.WriteStartElement("Green");
                    xml.WriteString(color.G.ToString());
                    xml.WriteEndElement();

                    xml.WriteStartElement("Blue");
                    xml.WriteString(color.B.ToString());
                    xml.WriteEndElement();

                    xml.WriteEndElement();
                }

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

            xml.WriteStartElement("TCPPort");
            xml.WriteString(TCPPort.ToString());
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
                        string comPort = "COM0";
                        string overlappedMusicMode = null;
                        string syncedLedItem = "DONTSYNC";

                        int baudRate = 4800;

                        byte type = 0;
                        byte ledCount = 0;
                        byte dPin = 0;
                        byte rPin = 0;
                        byte gPin = 0;
                        byte bPin = 0;

                        byte staticType = 0;
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
                        bool musicUseExponential = true;

                        Color staticModeColor = Color.FromRgb(255, 0, 0);
                        Color lightningModeColor = Color.FromRgb(255, 0, 0);
                        Color spinnerModeSpinnerColor = Color.FromRgb(255, 0, 0);
                        Color spinnerModeBackgroundColor = Color.FromRgb(255, 255, 255);

                        List<Color> ledColorList = new List<Color>();

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
                            else if (valueNode.Name == "SyncModeSyncedLedItem")
                            {
                                syncedLedItem = valueNode.InnerText;
                            }
                            else if (valueNode.Name == "OverlappedMusicMode")
                            {
                                overlappedMusicMode = valueNode.InnerText;
                            }
                            else if (valueNode.Name == "MusicUseExponential")
                            {
                                musicUseExponential = Convert.ToBoolean(valueNode.InnerText);
                            }
                            else if (valueNode.Name == "On")
                            {
                                on = Convert.ToBoolean(valueNode.InnerText);
                            }
                            else if(valueNode.Name == "StaticType")
                            {
                                staticType = Convert.ToByte(valueNode.InnerText);
                            }
                            else if(valueNode.Name == "LedColorList")
                            {
                                //get the color of every LED
                                foreach (XmlNode ledColorNode in valueNode)
                                {
                                    byte red = 0;
                                    byte green = 0;
                                    byte blue = 0;

                                    foreach(XmlNode colorNode in ledColorNode)
                                    {
                                        switch(colorNode.Name)
                                        {
                                            case "Red":
                                                red = Convert.ToByte(colorNode.InnerText);
                                                break;

                                            case "Green":
                                                green = Convert.ToByte(colorNode.InnerText);
                                                break;

                                            case "Blue":
                                                blue = Convert.ToByte(colorNode.InnerText);
                                                break;
                                        }
                                    }

                                    ledColorList.Add(Color.FromRgb(red, green, blue));
                                }
                            }
                            else
                            {
                                PutNotification("An error occured while loading the configuration. If the program still works, this message can be ignored.");
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
                            SpinnerModeBackgroundColorBrightness = spinnerModeBackgroundColorBrightness,
                            SyncedLedItem = syncedLedItem,
                            MusicUseExponential = musicUseExponential,
                            StaticType = staticType,
                            LedColorList = ledColorList
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
                        else if (subNode.Name == "TCPPort")
                        {
                            TCPPort = Convert.ToInt32(subNode.InnerText);
                        }
                    }

                    AccentColor = Color.FromRgb(red, green, blue);
                }
                else
                {
                    PutNotification("An error occured while loading the configuration. If the program still works, this message can be ignored.");
                }
            }

            //select the correct mode
            if (SelectedLedItem != null)
                SelectedMode = SelectedLedItem.CurrentMode;

            MadeChanges = false;
        }

        private void sendDataToArduino(LedItem ledItem, string mode, bool setLedItemClassArgs)
        {
            try
            {
                ledItem.InitSerial();

                //reset the syncedLed if it isnt using syncmode
                if(mode != "Sync")
                    ledItem.SyncedLedItem = "DONTSYNC";

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

                bool useMulticolor = false;

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

                        if (modeStatic.Type == 1 && ledItem.Type == 0)
                            useMulticolor = true;

                        //set the arguments in the leditem class
                        if (setLedItemClassArgs)
                        {
                            ledItem.StaticModeColor = modeStatic.SelectedColor;
                            ledItem.StaticBrightness = modeStatic.Brightness;
                            ledItem.StaticType = modeStatic.Type;
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

                        //set the arguments for the usb message (Cannot just use the realcolor property here because of a invalidopertationexception I Cannot fix)
                        arg1 = (byte)((float)modeLightning.SelectedColor.R * ((float)modeLightning.Brightness / 255.00));
                        arg2 = (byte)((float)modeLightning.SelectedColor.G * ((float)modeLightning.Brightness / 255.00));
                        arg3 = (byte)((float)modeLightning.SelectedColor.B * ((float)modeLightning.Brightness / 255.00));

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

                    case "Sync":
                        tmpMode = "SYNC";

                        //set the arguments for the usb message
                        foreach(LedItem item in LedItem.AllItems)
                        {
                            if (item.ItemName == modeSync.SyncedLedItem)
                                arg1 = (byte)item.ID;
                        }

                        //set the arguments in the leditem class
                        if(setLedItemClassArgs)
                        {
                            ledItem.SyncedLedItem = modeSync.SyncedLedItem;
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

                if(ledItem.IsNetwork)
                {
                    List<HTTPAttribute> attrs = new List<HTTPAttribute>();
                    attrs.Add(new HTTPAttribute("Command", message));

                    string res = SendGetRequest(ledItem.IpAddress, ledItem.ServerPort, attrs, TCPTimeout);

                    if (res == TCPCOULDNOTCONNECT.ToString())
                        PutNotification($"Cannot connect to your ESP32 at {ledItem.IpAddress}! Please check if it received a new IP-Address.");
                    else if(res == TCPINVALIDCOMMAND.ToString())
                        PutNotification($"Internal error while sending data to {ledItem.IpAddress}");

                    return;
                }

                if(useMulticolor)
                {
                    for(int x = 0; x < ledItem.LedColorList.Count; x++)
                    {
                        message = $"&{ledItem.ID.ToString("D2")}{pins}{x.ToString("D3")}";
                        message += $"{((byte)((float)ledItem.LedColorList[x].R * ((float)modeStatic.Brightness / 255.00))).ToString("D3")}{(ledItem.LedColorList[x].G * (modeStatic.Brightness / 255)).ToString("D3")}{(ledItem.LedColorList[x].B * (modeStatic.Brightness / 255)).ToString("D3")}";
                        message += "\\n";

                        ledItem.SerialWrite(message);

                        int delay = 0;

                        DateTime timeAtStart = DateTime.UtcNow;

                        while (!ledItem.CorrespondingArduino.IsOk)
                        {
                            Thread.Sleep(1);
                            delay++;

                            // check if the Arduino is not responding for 3 seconds
                            if (DateTime.UtcNow.Subtract(timeAtStart).TotalMilliseconds >= 3000)
                                throw new ArduinoNotRespondingException();
                        }

                        ledItem.CorrespondingArduino.IsOk = false;
                    }
                }
                else
                {
                    ledItem.SerialWrite(message);
                }

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    logBoxText.Text += $"Send: {message} using BAUD-{ledItem.BaudRate} on {ledItem.ComPortName}";
                    logBoxText.Text += Environment.NewLine;
                }));
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(UnauthorizedAccessException))
                    PutNotification($"Cannot write to {ledItem.ComPortName} (The Port may already be in use)");
                else if (ex.GetType() == typeof(NoSerialPortSelectedException))
                    PutNotification($"No COM-Port is selected for {ledItem.ItemName}");
                else if (ex.GetType() == typeof(ArduinoNotRespondingException))
                    PutNotification($"The Arduino at {ledItem.ComPortName} is not responding");
                else
                    PutNotification($"Cannot write to {ledItem.ComPortName}  (The Arduino might be disconnected or connected to another port)");

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    logBoxText.Text += $"Cannot write to {ledItem.ComPortName} ({ ex.GetType().Name })";
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

        private void ModeTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            this.Topmost = true;
            this.Activate();

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!firstLoad) return;

            firstLoad = false;

            if (App.Args != null)
            {
                foreach (string s in App.Args)
                {
                    if (s == "-startup")
                        this.Hide();
                }
            }
        }

        private void Icon_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            iconPressed++;

            if(iconPressed == 8)
            {
                iconPressed = 0;

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

        private void DismissNotification_Click(object sender, RoutedEventArgs e)
        {
            if (notificationList.Count == 1)
            {
                ShowNotification = false;
                notificationList.Clear();

                return;
            }

            // show the next notification
            notificationList.Remove(NotificationText);
            NotificationText = notificationList[0];
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

        public static string GUID
        {
            get { return "{1B3CB67C-6ADF-46DA-8740-752AFC4BA540}"; }
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

                    StartBassNet = true;
                    EnableLogBox = false;
                    BlockPopups = false;
                    TCPPort = 39769;
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
            get { return enableLogBox; }
            set
            {
                enableLogBox = value;
                OnPropertyChanged("EnableLogBox");
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
                            new AlertWindow("Cannot connect to the internet!").ShowDialog();

                            ShowArduinoUploadWindow = false;

                            logBoxText.Text += "Cannot connect to the internet (Unable to connect to 'http://google.com/generate_204')";
                            logBoxText.Text += Environment.NewLine;
                        }));
                    }
                }))
                { ApartmentState = ApartmentState.STA, IsBackground = true }.Start();
            }
        }

        public bool ShowMultiColorEditorWindow
        {
            get { return showMultiColorEditorWindow; }
            set 
            { 
                showMultiColorEditorWindow = value;
                OnPropertyChanged("ShowMultiColorEditorWindow");
            }
        }

        public bool SyncAvailable
        {
            get { return syncAvailable; }
            set
            {
                syncAvailable = value;
                OnPropertyChanged("SyncAvailable");
            }
        }

        public bool ShowNotification
        {
            get { return showNotification; }
            set
            {
                showNotification = value;
                OnPropertyChanged("ShowNotification");
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

        public string NotificationText
        {
            get { return notificationText; }
            set
            {
                notificationText = value;
                OnPropertyChanged("NotificationText");
            }
        }

        public int TCPPort
        {
            get { return tcpPort; }
            set
            {
                tcpPort = value;
                OnPropertyChanged("TCPPort");
            }
        }

        public int TCPTimeout
        {
            get { return tcpTimeout; }
            set 
            {
                tcpTimeout = value;
                OnPropertyChanged("TCPTimeout");
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

                    modeStatic.Brightness = SelectedLedItem.StaticBrightness;
                    modeStatic.SelectedColor = SelectedLedItem.StaticModeColor;
                    modeStatic.Type = SelectedLedItem.StaticType;
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
                    modeSync.SelectedItemSyncableItems = SelectedLedItem.SyncableItems;
                    modeMusic.OverlappedMode = SelectedLedItem.OverlappedMusicMode;
                    modeMusic.UseExponential = SelectedLedItem.MusicUseExponential;

                    if (SelectedLedItem.SyncedLedItem != "DONTSYNC")
                        modeSync.SyncedLedItem = SelectedLedItem.SyncedLedItem;

                    //make sync mode unavailable if syncableitems is empty
                    if (SelectedLedItem.SyncableItems.Count == 0)
                        SyncAvailable = false;
                    else
                        SyncAvailable = true;
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

        //tcp listener codes
        public static int TCPREQUESTOK
        {
            get { return 200; }
        }

        public static int TCPINVALIDCOMMAND
        {
            get { return 400; }
        }

        public static int TCPCOULDNOTCONNECT
        {
            get { return 401; }
        }

        public static int TCPLEDNOTFOUND
        {
            get { return 404; }
        }

        public static int TCPNOCONNECTEDLEDS
        {
            get { return 300; }
        }

        public static string TCPSETLED
        {
            get { return "SETLED"; }
        }

        public static string TCPGETINFO
        {
            get { return "GETINFO"; }
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

    public class ArduinoNotRespondingException : Exception
    {
        public ArduinoNotRespondingException()
        {
        }

        public ArduinoNotRespondingException(string message) : base(message)
        {
        }

        public ArduinoNotRespondingException(string message, Exception inner) : base(message, inner)
        {
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