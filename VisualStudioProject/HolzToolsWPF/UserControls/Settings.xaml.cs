using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Text.RegularExpressions;

namespace HolzTools.UserControls
{
    public partial class Settings : INotifyPropertyChanged
    {
        private Color selectedAccentColor;

        private bool madeChanges = false;
        private bool isDefault = false;
        private bool selectedAutoUpdate = MainWindow.ActiveWindow.AutoUpdate;
        private bool selectedEnableFanAnim = MainWindow.ActiveWindow.EnableFanAnim;
        private bool selectedEnableLogBox = MainWindow.ActiveWindow.EnableLogBox;
        private bool selectedBlockPopups = MainWindow.ActiveWindow.BlockPopups;
        private bool selectedStartBassNet = MainWindow.ActiveWindow.StartBassNet;
        private bool selectedAutoStart;
       
        private int selectedTCPPort = MainWindow.ActiveWindow.TCPPort;

        private static readonly Regex _regex = new Regex("[^0-9]+");

        public Settings()
        {
            InitializeComponent();
            DataContext = this;

            //check if the application is automatically starting
            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), $"{MainWindow.ApplicationName}.lnk")))
                SelectedAutoStart = true;

            MainWindow.ActiveWindow.settingsBackgroundGrid.MouseUp += CancelBtn_Click;

            //make dev settings available if they are enabled
            if (MainWindow.ActiveWindow.IsDev)
                devSettings.Visibility = Visibility.Visible;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        //events
        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void DefaultColorButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            SolidColorBrush brush = (SolidColorBrush)btn.Background;
            SelectedAccentColor = brush.Color;
        }

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
                    MainWindow.ActiveWindow.ShowSettings = false;

                    //remove the event handler from settingsgrid
                    MainWindow.ActiveWindow.settingsBackgroundGrid.MouseUp -= CancelBtn_Click;
                }
            }
            else
            {
                MainWindow.ActiveWindow.ShowSettings = false;

                //remove the event handler from settingsgrid
                MainWindow.ActiveWindow.settingsBackgroundGrid.MouseUp -= CancelBtn_Click;
            }
        }

        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ActiveWindow.TCPPort != SelectedTCPPort)
                new AlertWindow("You must restart the application to make the port change take effect.").ShowDialog();

            //save the settings
            MainWindow.ActiveWindow.AutoUpdate = SelectedAutoUpdate;
            MainWindow.ActiveWindow.AccentColor = SelectedAccentColor;
            MainWindow.ActiveWindow.EnableFanAnim = SelectedEnableFanAnim;
            MainWindow.ActiveWindow.EnableLogBox = SelectedEnableLogBox;
            MainWindow.ActiveWindow.BlockPopups = SelectedBlockPopups;
            MainWindow.ActiveWindow.StartBassNet = SelectedStartBassNet;
            MainWindow.ActiveWindow.TCPPort = SelectedTCPPort;

            //set the autostart shortcut
            if (!System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), $"{MainWindow.ApplicationName}.lnk")) && SelectedAutoStart)
            {
                CreateShortcut(MainWindow.ApplicationName, Environment.GetFolderPath(Environment.SpecialFolder.Startup), MainWindow.InstallLocation, Path.Combine(MainWindow.InstallLocation, "icon.ico"), "-startup");
            }
            else if(System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), $"{MainWindow.ApplicationName}.lnk")) && !SelectedAutoStart)
            {
                System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), $"{MainWindow.ApplicationName}.lnk"));
            }

            MainWindow.ActiveWindow.ShowSettings = false;

            //remove the event handler from settingsgrid
            MainWindow.ActiveWindow.settingsBackgroundGrid.MouseUp -= CancelBtn_Click;
        }
        private void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation, string iconLocation, string arguments = "")
        {
            string shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = "Launch " + MainWindow.ApplicationName;
            shortcut.IconLocation = iconLocation;
            shortcut.TargetPath = targetFileLocation;
            shortcut.Arguments = arguments;
            shortcut.Save();
        }

        private void CustomColorBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.colorPickerViewBox.Child = new CustomColorPicker(SelectedAccentColor, CustomColorPicker.ColorToBeChanged.SettingsAccentColor);
            MainWindow.ActiveWindow.ShowColorPicker = true;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SelectedAccentColor = MainWindow.ActiveWindow.AccentColor;
            MadeChanges = false;
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            new Thread(new ThreadStart(() =>
            {
                MainWindow.ActiveWindow.CheckForUpdate(true);
            }))
            { ApartmentState = ApartmentState.STA, IsBackground = true }.Start();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            new TermsAndConditionsWindow().ShowDialog();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        //getters and setters
        public Color SelectedAccentColor
        {
            get { return selectedAccentColor; }
            set
            {
                isDefault = false;

                customColorBtn.Tag = "NotSelected";

                //select the new button
                foreach (Button defaultColorBtn in FindVisualChildren<Button>(this))
                {
                    SolidColorBrush background = (SolidColorBrush)defaultColorBtn.Background;

                    if (background.Color == SelectedAccentColor && background.Color != value)
                    {
                        defaultColorBtn.Tag = "NotSelected";
                    }
                    else if (background.Color == value)
                    {
                        defaultColorBtn.Tag = "Selected";
                        isDefault = true;
                    }
                }

                if (!isDefault)
                {
                    customColorBtn.Tag = "Selected";
                }

                selectedAccentColor = value;
                OnPropertyChanged("SelectedAccentColor");

                MadeChanges = true;
            }
        }

        public bool SelectedAutoUpdate
        {
            get { return selectedAutoUpdate; }
            set
            {
                selectedAutoUpdate = value;
                OnPropertyChanged("SelectedAutoUpdate");

                MadeChanges = true;
            }
        }

        public bool SelectedEnableFanAnim
        {
            get { return selectedEnableFanAnim; }
            set
            {
                selectedEnableFanAnim = value;
                OnPropertyChanged("SelectedEnableFanAnim");

                MadeChanges = true;
            }
        }

        public bool SelectedEnableLogBox
        {
            get { return selectedEnableLogBox; }
            set
            {
                selectedEnableLogBox = value;
                OnPropertyChanged("SelectedEnableLogBox");

                MadeChanges = true;
            }
        }

        public bool SelectedBlockPopups
        {
            get { return selectedBlockPopups; }
            set
            {
                selectedBlockPopups = value;
                OnPropertyChanged("SelectedBlockPopups");

                MadeChanges = true;
            }
        }

        public bool SelectedStartBassNet
        {
            get { return selectedStartBassNet; }
            set
            {
                selectedStartBassNet = value;
                OnPropertyChanged("SelectedStartBassNet");

                MadeChanges = true;
            }
        }

        public bool SelectedAutoStart
        {
            get { return selectedAutoStart; }
            set
            {
                selectedAutoStart = value;
                OnPropertyChanged("SelectedAutoStart");

                MadeChanges = true;
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

        public int SelectedTCPPort
        {
            get { return selectedTCPPort; }
            set
            {
                selectedTCPPort = value;
                OnPropertyChanged("SelectedTCPPort");

                MadeChanges = true;
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