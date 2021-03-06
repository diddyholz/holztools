﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HolzTools
{
    public partial class ArduinoBinaryDownloaderWindow : Window
    {
        public ArduinoBinaryDownloaderWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        //events
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
            window.Activate();
        }

        private void DownloadFinished(object sender, RoutedEventArgs e)
        {
            DoubleAnimation closeAnim = new DoubleAnimation()
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(100))
            };

            closeAnim.Completed += ClosingStoryboard_Completed;
            this.BeginAnimation(Window.OpacityProperty, closeAnim);
        }

        private void ClosingStoryboard_Completed(object sender, EventArgs e)
        {
            this.Close();
        }

        //getters and setters
        public Color AccentColor
        {
            get { return MainWindow.ActiveWindow.AccentColor; }
        }

        //event to make bindings work
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
