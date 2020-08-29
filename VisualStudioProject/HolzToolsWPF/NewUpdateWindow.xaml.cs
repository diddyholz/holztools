using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
    /// <summary>
    /// Interaction logic for NewUpdateWindow.xaml
    /// </summary>
    public partial class NewUpdateWindow
    {
        bool value = false;
        bool isArduinoUpdate = false;

        string newVersion = "PLACEHOLDER";

        ObservableCollection<string> fixList = new ObservableCollection<string>();          //the list that contains all fixes
        ObservableCollection<string> featureList = new ObservableCollection<string>();      //the list that contains all new features
        ObservableCollection<string> optimizeList = new ObservableCollection<string>();     //the list that contains all optimizations

        public NewUpdateWindow(string _newVersion, string _changelogString, bool _isArduinoUpdate = false, byte _updatableArduinos = 0)
        {
            newVersion = _newVersion;
            isArduinoUpdate = _isArduinoUpdate;

            fixList.CollectionChanged += FixList_CollectionChanged;
            featureList.CollectionChanged += FeatureList_CollectionChanged;
            optimizeList.CollectionChanged += OptimizeList_CollectionChanged;

            InitializeComponent(); bool isTypeAttribute = false;
            bool isFistChar = false;

            ObservableCollection<string> activeList = null;

            string change = "";

            foreach (char c in _changelogString)
            {
                if (c == '(' && !isTypeAttribute)
                {
                    //gets the next char as a type attribute
                    isTypeAttribute = true;
                }
                else if (c == '(' && isTypeAttribute)
                {
                    change += c;
                    isTypeAttribute = false;
                }
                else if (isTypeAttribute)
                {
                    //sets the current change to a list
                    if (change != "" && activeList != null)
                    {
                        activeList.Add(change);
                    }

                    switch (c)
                    {
                        case 'f':
                            activeList = FixList;
                            break;

                        case 'n':
                            activeList = FeatureList;
                            break;

                        case 'o':
                            activeList = OptimizeList;
                            break;
                    }

                    isTypeAttribute = false;
                }
                else if (c == ')' && !isFistChar)
                {
                    isFistChar = true;
                }
                else if (c == ')' && isFistChar)
                {
                    change += c;
                    isFistChar = false;
                }
                else if (isFistChar)
                {
                    change = c.ToString();
                    isFistChar = false;
                }
                else
                {
                    change += c;
                }
            }

            if (change != "" && activeList != null)
            {
                activeList.Add(change);
            }

            if (IsArduinoUpdate)
                yesBtn.Content = $"Update {_updatableArduinos} Arduino/s";

            DataContext = this;
        }

        //events
        private void OptimizeList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            optimizeGrid.Visibility = Visibility.Visible;
        }

        private void FeatureList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            featureGrid.Visibility = Visibility.Visible;
        }

        private void FixList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            fixGrid.Visibility = Visibility.Visible;
        }

        private void YesNoBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;

            if (btn.Name == "yesBtn")
                value = true;
            else
                value = false;

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
            if (this.DialogResult == null)
                this.DialogResult = value;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ActiveWindow.BlockPopups)
                this.DialogResult = false;
            else
                System.Media.SystemSounds.Asterisk.Play();

            Window window = (Window)sender;
            window.Topmost = true;
            window.Activate();
        }

        //getters and setters
        public string NewVersion
        {
            get
            {
                return newVersion;
            }
        }

        public bool IsArduinoUpdate
        {
            get
            {
                return isArduinoUpdate;
            }
        }

        public ObservableCollection<string> FixList
        {
            get { return fixList; }
        }

        public ObservableCollection<string> FeatureList
        {
            get { return featureList; }
        }

        public ObservableCollection<string> OptimizeList
        {
            get { return optimizeList; }
        }

        public Color AccentColor
        {
            get { return MainWindow.ActiveWindow.AccentColor; }
        }
    }
}
