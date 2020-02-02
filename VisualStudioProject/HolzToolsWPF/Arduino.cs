using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HolzTools
{
    public class Arduino
    {
        private SerialPort activeSerialPort;

        private string serialPortName;
        private string binaryVersion = "";

        private Type arduinoType;

        private int baudRate = 4800;

        public static List<Arduino> AllArduinos = new List<Arduino>();

        public enum Type
        {
            NanoR3 = 5,
            UnoR3 = 6
        }

        public Arduino()
        {
            AllArduinos.Add(this);
        }

        public void InitializePort()
        {
            if(ActiveSerialPort != null)
                if(!ActiveSerialPort.IsOpen)
                    ActiveSerialPort.Open();

            activeSerialPort.DataReceived += ActiveSerialPort_DataReceived;

            ActiveSerialPort.Write("69");
        }

        //events
        private void ActiveSerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string message = activeSerialPort.ReadExisting();

            //sets the binary version and arduino model when the message starts with a _
            if (message.StartsWith("_"))
            {
                BinaryVersion = message.Split('_')[1];

                switch (message.Split('_')[2])
                {
                    case "NanoR3":
                        ArduinoType = Type.NanoR3;
                        break;

                    case "UnoR3":
                        ArduinoType = Type.UnoR3;
                        break;
                }

                MainWindow.ActiveWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MainWindow.ActiveWindow.logBoxText.Text += $"Set Binary Version and Model of Arduino at {SerialPortName} to {BinaryVersion} and {ArduinoType}";
                    MainWindow.ActiveWindow.logBoxText.Text += Environment.NewLine;
                }));
            }
            else
            {
                MainWindow.ActiveWindow.Dispatcher.BeginInvoke(new Action(() => { MainWindow.ActiveWindow.logBoxText.Text += message; }));
            }
        }

        //getters and setters
        public int BaudRate
        {
            get { return baudRate; }
            set
            {
                if (value != 0 && value != baudRate)
                {
                    baudRate = value;
                    ActiveSerialPort = new SerialPort(SerialPortName, value, Parity.None, 8, StopBits.One);
                }
            }
        }

        public string SerialPortName
        {
            get { return serialPortName; }
            set
            {
                if ((value != null && value != "") && value != serialPortName)
                {
                    serialPortName = value;
                    ActiveSerialPort = new SerialPort(value, BaudRate, Parity.None, 8, StopBits.One);
                }
            }
        }

        public string BinaryVersion
        {
            get { return binaryVersion; }
            set { binaryVersion = value; }
        }

        public SerialPort ActiveSerialPort
        {
            get { return activeSerialPort; }
            set
            {
                if(activeSerialPort != null)
                    if(activeSerialPort.IsOpen)
                        activeSerialPort.Close();

                activeSerialPort = value;
            }
        }

        public Type ArduinoType
        {
            get { return arduinoType; }
            set { arduinoType = value; }
        }
    }
}
