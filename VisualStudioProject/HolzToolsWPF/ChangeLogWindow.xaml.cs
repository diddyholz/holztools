using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace HolzTools
{
    public partial class ChangeLogWindow : Window
    {
        ObservableCollection<string> fixList = new ObservableCollection<string>();          //the list that contains all fixes
        ObservableCollection<string> featureList = new ObservableCollection<string>();      //the list that contains all new features
        ObservableCollection<string> optimizeList = new ObservableCollection<string>();     //the list that contains all optimizations

        private string binaryVersion = "";

        public ChangeLogWindow(string changelogString = "(i)No Changelog Available", string binaryVersion = "")
        {
            InitializeComponent();

            fixList.CollectionChanged += FixList_CollectionChanged;
            featureList.CollectionChanged += FeatureList_CollectionChanged;
            optimizeList.CollectionChanged += OptimizeList_CollectionChanged;

            bool isTypeAttribute = false;
            bool isFistChar = false;

            ObservableCollection<string> activeList = null;

            this.binaryVersion = binaryVersion;

            if (!string.IsNullOrEmpty(binaryVersion))
                versionText.Text = "Changelog for binary version ";

            string change = "";

            foreach (char c in changelogString)
            {
                if (c == '(' && !isTypeAttribute)
                {
                    //gets the next char as a type attribute
                    isTypeAttribute = true;
                }
                else if (c == '(' && isTypeAttribute)
                {
                    //gets the next char as a type attribute
                    change += c;
                    isTypeAttribute = false;
                }
                else if (isTypeAttribute)
                {
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

                    //sets the current change to a list
                    if (change != "" && activeList != null)
                    {
                        activeList.Add(change);
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

            DataContext = this;
        }

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

        private void OkBtn_Click(object sender, RoutedEventArgs e)
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

        public string CurrentVersion
        {
            get 
            {
                if (binaryVersion != "")
                    return binaryVersion;

                return MainWindow.CurrentVersion; 
            }
        }
    }
}
