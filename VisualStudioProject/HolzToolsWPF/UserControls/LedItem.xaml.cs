﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using System.Collections.Generic;

namespace HolzTools.UserControls
{
    public partial class LedItem : INotifyPropertyChanged
    {
        //list of all items
        private static ObservableCollection<UserControls.LedItem> allItems = new ObservableCollection<LedItem>();

        private int type = 0;   //0 for ARGB, 1 for 4Pin RGB
        private int ledCount = 10;
        private int dPin = 0;
        private int rPin = 0;
        private int gPin = 0;
        private int bPin = 0;
        private int id = 0;
        private int musicFrequency = 2;
        private int isMusic = 0;
        private int baudRate = 4800;

        private byte staticBrightness = 255;
        private byte cycleBrightness = 255;
        private byte rainbowBrightness = 255;
        private byte lightningBrightness = 255;
        private byte cycleSpeed = 0;
        private byte rainbowSpeed = 0;
        private byte overlaySpeed = 0;
        private byte overlayDirection = 0;
        private byte spinnerSpeed = 0;
        private byte spinnerLength = 0;
        private byte spinnerColorBrightness = 255;
        private byte backgroundColorBrightness = 255;

        private bool isOn = true;
        private bool musicUseExponential = true;

        private string itemName = "LED";
        private string mode = "Static";
        private string overlappedMusicMode = "Static";
        private string comPort = "COM0";
        private string syncedLedItem = "";

        private Color staticModeColor = Color.FromRgb(255, 0, 0);
        private Color lightningModeColor = Color.FromRgb(255, 0, 0);
        private Color spinnerModeSpinnerColor = Color.FromRgb(255, 0, 0);
        private Color spinnerModeBackgroundColor = Color.FromRgb(255, 255, 255);

        private Arduino arduino;

        private List<string> syncableItems = new List<string>();

        public LedItem()
        {
            AllItems.Add(this);

            MainWindow.ActiveWindow.itemStackPanel.Children.Add(this);

            Name = $"Item{LedItem.AllItems.Count}";

            foreach(LedItem item in LedItem.AllItems)
            {
                if (item.ItemName == "LED")
                    ItemName = $"LED{LedItem.AllItems.Count}";
            }

            ID = MainWindow.IDCounter;

            LedItem.RefreshSyncableItems();

            //make item selected if its the first
            if (LedItem.AllItems.Count == 1)
                MainWindow.ActiveWindow.SelectedLedItem = this;

            MainWindow.IDCounter++;

            DataContext = this;

            InitializeComponent();
        }

        public static void RefreshSyncableItems()
        {
            foreach(LedItem item in LedItem.AllItems)
            {
                List<string> tempSyncableItemList = new List<string>();

                foreach (LedItem syncItem in LedItem.AllItems)
                {
                    //check if the syncitem can be added as a syncable item to the leditem
                    if ((item.ID != syncItem.ID) && (item.CorrespondingArduino == syncItem.CorrespondingArduino) && (syncItem.SyncedLedItem != item.ItemName))
                        tempSyncableItemList.Add(syncItem.ItemName);
                }

                item.SyncableItems = tempSyncableItemList;
            }

            if(MainWindow.ActiveWindow.SelectedLedItem != null)
                MainWindow.ActiveWindow.modeSync.SelectedItemSyncableItems = MainWindow.ActiveWindow.SelectedLedItem.SyncableItems;
        }

        public void Delete()
        {
            MainWindow.ActiveWindow.itemStackPanel.Children.Remove(this);

            AllItems.Remove(this);

            LedItem.RefreshSyncableItems();

            //display no led message if this was the last led
            if (LedItem.AllItems.Count != 0)
            {
                //select the next item if this item was selected
                if (MainWindow.ActiveWindow.SelectedLedItem == this)
                    MainWindow.ActiveWindow.SelectedLedItem = AllItems[0];

                bool foundport = false;

                foreach (LedItem item in LedItem.AllItems)
                {
                    if (item.ComPortName == ComPortName)
                    {
                        foundport = true;
                    }
                }

                if (!foundport)
                {
                    try
                    {
                        arduino.ActiveSerialPort.Close();
                    }
                    catch { }

                    Arduino.AllArduinos.Remove(arduino);
                }
            }
            else
            {
                MainWindow.ActiveWindow.SelectedLedItem = null;
            }

            //If this led was the syncparent of any other led, remove that sync
            foreach(LedItem item in LedItem.AllItems)
            {
                if(item.SyncedLedItem == this.ItemName)
                {
                    item.SyncedLedItem = "DONTSYNC";
                    item.CurrentMode = "Static";

                    if (item == MainWindow.ActiveWindow.SelectedLedItem)
                        MainWindow.ActiveWindow.SelectedMode = "Static";
                }
            }
        }

        public void InitSerial()
        {
            arduino.InitializePort();
        }

        public void CloseSerial()
        {
            try
            {
                arduino.ActiveSerialPort.Close();
            }
            catch
            { }
        }

        public void SerialWrite(string message)
        {
            if(arduino.ActiveSerialPort.IsOpen == true)
                arduino.ActiveSerialPort.Write(message);
        }

        //events
        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (MainWindow.ActiveWindow.SelectedLedItem != this)
                MainWindow.ActiveWindow.SelectedLedItem = this;
        }

        private void ItemPropertyBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.propertiesViewbox.Child = new LedItemProperties(ItemName, Type, ComPortName, LedCount, DPIN, RPIN, GPIN, BPIN, BaudRate, this);

            MainWindow.ActiveWindow.ShowProperties = true;
        }

        //getters and setters
        public static ObservableCollection<UserControls.LedItem> AllItems
        {
            get { return allItems; }
            set { allItems = value; }
        }

        public List<string> SyncableItems
        {
            get { return syncableItems; }
            set
            {
                syncableItems = value;
                OnPropertyChanged("SyncableItems");
            }
        }

        public string ItemName
        {
            get { return itemName; }
            set
            {
                if (value != null && value != "")
                {
                    itemName = value;
                    OnPropertyChanged("ItemName");

                    LedItem.RefreshSyncableItems();
                }
            }
        }

        public Color StaticModeColor
        {
            get { return staticModeColor; }
            set
            {
                staticModeColor = value;
                OnPropertyChanged("StaticModeColor");
            }
        }

        public Color LightningModeColor
        {
            get { return lightningModeColor; }
            set
            {
                lightningModeColor = value;
                OnPropertyChanged("LightningModeColor");
            }
        }

        public Color SpinnerModeSpinnerColor
        {
            get { return spinnerModeSpinnerColor; }
            set
            {
                spinnerModeSpinnerColor = value;
                OnPropertyChanged("SpinnerModeSpinnerColor");
            }
        }

        public Color SpinnerModeBackgroundColor
        {
            get { return spinnerModeBackgroundColor; }
            set
            {
                spinnerModeBackgroundColor = value;
                OnPropertyChanged("SpinnerModeBackgroundColor");
            }
        }

        public byte StaticBrightness
        {
            get { return staticBrightness; }
            set
            {
                staticBrightness = value;
                OnPropertyChanged("StaticBrightness");
            }
        }

        public byte CycleBrightness
        {
            get { return cycleBrightness; }
            set
            {
                cycleBrightness = value;
                OnPropertyChanged("CycleBrightness");
            }
        }

        public byte RainbowBrightness
        {
            get { return rainbowBrightness; }
            set
            {
                rainbowBrightness = value;
                OnPropertyChanged("RainbowBrightness");
            }
        }

        public byte LightningBrightness
        {
            get { return lightningBrightness; }
            set
            {
                lightningBrightness = value;
                OnPropertyChanged("RainbowBrightness");
            }
        }

        public byte CycleSpeed
        {
            get { return cycleSpeed; }
            set
            {
                cycleSpeed = value;
                OnPropertyChanged("CycleSpeed");
            }
        }

        public byte RainbowSpeed
        {
            get { return rainbowSpeed; }
            set
            {
                rainbowSpeed = value;
                OnPropertyChanged("RainbowSpeed");
            }
        }

        public byte OverlaySpeed
        {
            get { return overlaySpeed; }
            set
            {
                overlaySpeed = value;
                OnPropertyChanged("OverlaySpeed");
            }
        }

        public byte OverlayDirection
        {
            get { return overlayDirection; }
            set
            {
                overlayDirection = value;
                OnPropertyChanged("OverlayDirection");
            }
        }

        public byte SpinnerSpeed
        {
            get { return spinnerSpeed; }
            set
            {
                spinnerSpeed = value;
                OnPropertyChanged("SpinnerSpeed");
            }
        }

        public byte SpinnerLength
        {
            get { return spinnerLength; }
            set
            {
                spinnerLength = value;
                OnPropertyChanged("SpinnerLength");
            }
        }

        public byte SpinnerModeSpinnerColorBrightness
        {
            get { return spinnerColorBrightness; }
            set
            {
                spinnerColorBrightness = value;
                OnPropertyChanged("SpinnerModeSpinnerColorBrightness");
            }
        }

        public byte SpinnerModeBackgroundColorBrightness
        {
            get { return backgroundColorBrightness; }
            set
            {
                backgroundColorBrightness = value;
                OnPropertyChanged("SpinnerModeBackgroundColorBrightness");
            }
        }

        public int DPIN
        {
            get { return dPin; }
            set
            {
                dPin = value;
                OnPropertyChanged("DPIN");
            }
        }

        public int RPIN
        {
            get { return rPin; }
            set
            {
                rPin = value;
                OnPropertyChanged("RPIN");
            }
        }

        public int GPIN
        {
            get { return gPin; }
            set
            {
                gPin = value;
                OnPropertyChanged("GPIN");
            }
        }

        public int BPIN
        {
            get { return bPin; }
            set
            {
                bPin = value;
                OnPropertyChanged("BPIN");
            }
        }

        public int Type
        {
            get { return type; }
            set
            {
                type = value;
                OnPropertyChanged("Type");
            }
        }

        public string ComPortName
        {
            get { return comPort; }
            set
            {
                comPort = value;
                OnPropertyChanged("ComPortName");

                foreach (Arduino c in Arduino.AllArduinos)
                {
                    if (c.SerialPortName == value)
                    {
                        CorrespondingArduino = c;
                    }
                }

                if (CorrespondingArduino == null)
                {
                    CorrespondingArduino = new Arduino();
                }

                try
                {
                    InitSerial();
                }
                catch { }

                LedItem.RefreshSyncableItems();
            }
        }

        public int BaudRate
        {
            get 
            {
                //check if the item is allready belonging to an Arduino
                if (arduino != null)
                {
                    //check if the baud rate selected for the arduino is same for the led item
                    if (arduino.BaudRate != baudRate)
                        baudRate = arduino.BaudRate;
                }

                return baudRate; 
            }
            set
            {
                //check if the baud rate is valid
                if (value != 0)
                {
                    if (arduino != null)
                        arduino.BaudRate = value;

                    baudRate = value;
                }
            }
        }

        public int LedCount
        {
            get { return ledCount; }
            set
            {
                ledCount = value;
                OnPropertyChanged("LedCount");
            }
        }

        public int ID
        {
            get { return id; }
            set
            {
                id = value;
                OnPropertyChanged("ID");
            }
        }

        public int MusicFrequency
        {
            get { return musicFrequency; }
            set
            {
                musicFrequency = value;
                OnPropertyChanged("MusicFrequency");
            }
        }

        public bool IsOn
        {
            get { return isOn; }
            set
            {
                isOn = value;
                OnPropertyChanged("IsOn");
            }
        }

        public bool MusicUseExponential
        {
            get { return musicUseExponential; }
            set
            {
                musicUseExponential = value;
                OnPropertyChanged("MusicUseExponential");
            }
        }

        public int IsMusic
        {
            get { return isMusic; }
            set
            {
                isMusic = value;
                OnPropertyChanged("IsMusic");
            }
        }

        public string OverlappedMusicMode
        {
            get { return overlappedMusicMode; }
            set
            {
                overlappedMusicMode = value;
                OnPropertyChanged("OverlappedMusicMode");
            }
        }

        public string CurrentMode
        {
            get { return mode; }
            set
            {
                mode = value;
                OnPropertyChanged("CurrentMode");
            }
        }

        public string SyncedLedItem
        {
            get { return syncedLedItem; }
            set
            {
                syncedLedItem = value;
                OnPropertyChanged("SyncedLedItem");

                LedItem.RefreshSyncableItems();
            }
        }

        public Arduino CorrespondingArduino
        {
            get { return arduino; }
            set 
            {
                arduino = value;

                //check if the com port has the corresponding settings set
                arduino.SerialPortName = ComPortName;
                arduino.BaudRate = BaudRate;
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
