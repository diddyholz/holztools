using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Un4seen.Bass;
using Un4seen.BassWasapi;

namespace HolzTools
{
    public class Analyzer : INotifyPropertyChanged
    {
        private bool _enable;               //enabled status
        private Timer _t;                   //timer that refreshes the display
        private float[] _fft;               //buffer for fft data
        private WASAPIPROC _process;        //callback function to obtain data
        private int _lastlevel;             //last output level
        private int _hanctr;                //last output level counter
        private List<byte> _spectrumdata;   //spectrum data buffer
        private ComboBox _devicelist;       //device list
        private bool _initialized;          //initialized flag
        private int devindex;               //used device index
        private int devCount;

        private int _lines = 16;            // number of spectrum lines

        //ctor
        public Analyzer(ComboBox devicelist)
        {
            _fft = new float[1024];
            _lastlevel = 0;
            _hanctr = 0;
            _t = new Timer();
            _t.Elapsed += _t_Tick;
            _t.Interval = 17; //60hz refresh rate
            _t.Enabled = false;
            _process = new WASAPIPROC(Process);
            _spectrumdata = new List<byte>();
            _devicelist = devicelist;
            IsInitialized = false;

            devCount = BassWasapi.BASS_WASAPI_GetDeviceCount();
        }

        //flag for enabling and disabling program functionality
        public bool Enable
        {
            get { return _enable; }
            set
            {
                _enable = value;
                if (value)
                {
                    var array = MainWindow.ActiveWindow.modeMusic.SelectedDevice;
                    devindex = Convert.ToInt32(array[0]);
                    var flags = BASSWASAPIInit.BASS_WASAPI_AUTOFORMAT | BASSWASAPIInit.BASS_WASAPI_BUFFER;
                    bool result = BassWasapi.BASS_WASAPI_Init(devindex, 0, 0, flags, 1f, 0.05f, _process, IntPtr.Zero);
                    if (!result)
                    {
                        var error = Bass.BASS_ErrorGetCode();

                        string message = "";

                        AlertWindow alert = null;

                        if (error == BASSError.BASS_ERROR_FORMAT)
                        {
                            alert = new AlertWindow("The selected playback device is either using an unsupported sample rate or has audio enhancements enabled in the Windows Control Panel.", false);
                            message = $"Invalid format for device { array[0] }";
                        }
                        else if (error == BASSError.BASS_ERROR_ALREADY)
                        {
                            message = $"The audio device { array[0] } was already initialized";
                        }
                        else
                        {
                            alert = new AlertWindow("Unfortunately the BASS library has thrown an error. Check the log box for the error code.", false);
                            message = $"BASS.dll has thrown { error.ToString()}";
                        }

                        MainWindow.ActiveWindow.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MainWindow.ActiveWindow.logBoxText.Text += message;
                            MainWindow.ActiveWindow.logBoxText.Text += Environment.NewLine;
                        }));

                        if(alert != null)
                            alert.ShowDialog();
                    }

                    BassWasapi.BASS_WASAPI_Start();
                }
                else BassWasapi.BASS_WASAPI_Stop(true);
                System.Threading.Thread.Sleep(500);

                _t.Enabled = value;
            }
        }

        public void GetDevices()
        {
            for (int i = 0; i < devCount; i++)
            {
                var device = BassWasapi.BASS_WASAPI_GetDeviceInfo(i);

                if (device.IsEnabled && device.IsLoopback)
                {
                    _devicelist.Items.Add(string.Format("{0}-{1}", i, device.name));
                }
            }

            _devicelist.SelectedIndex = 0;
        }

        // initialization
        public void Init()
        {
            bool result = false;
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATETHREADS, false);
            result = Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            if (!result) throw new Exception("Init Error");

            IsInitialized = true;
        }

        //timer 
        public void _t_Tick(object sender, EventArgs e)
        {
            // get fft data. Return value is -1 on error
            int ret = BassWasapi.BASS_WASAPI_GetData(_fft, (int)BASSData.BASS_DATA_FFT2048);
            if (ret < 0) return;
            int x, y;
            int b0 = 0;

            //computes the spectrum data, the code is taken from a bass_wasapi sample.
            for (x = 0; x < _lines; x++)
            {
                float peak = 0;
                int b1 = (int)Math.Pow(2, x * 10.0 / (_lines - 1));
                if (b1 > 1023) b1 = 1023;
                if (b1 <= b0) b1 = b0 + 1;
                for (; b0 < b1; b0++)
                {
                    if (peak < _fft[1 + b0]) peak = _fft[1 + b0];
                }
                y = (int)(Math.Sqrt(peak) * 3 * 255 - 4);
                if (y > 255) y = 255;
                if (y < 0) y = 0;
                _spectrumdata.Add((byte)y);
            }

            MainWindow.ActiveWindow.modeMusic.SetIntensity(_spectrumdata);
            _spectrumdata.Clear();

            int level = BassWasapi.BASS_WASAPI_GetLevel();
            if (level == _lastlevel && level != 0) _hanctr++;
            _lastlevel = level;
        }

        // WASAPI callback, required for continuous recording
        private int Process(IntPtr buffer, int length, IntPtr user)
        {
            return length;
        }

        public bool IsInitialized
        {
            get { return _initialized; }
            set
            {
                _initialized = value;
                OnPropertyChanged("IsInitialized");
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