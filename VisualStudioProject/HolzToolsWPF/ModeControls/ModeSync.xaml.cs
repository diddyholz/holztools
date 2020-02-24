using HolzTools.UserControls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;

namespace HolzTools.ModeControls
{
    public partial class ModeSync : INotifyPropertyChanged
    {
        private List<string> allItemNames = new List<string>();

        private LedItem syncedLedItem;

        public ModeSync()
        {
            InitializeComponent();
            DataContext = this;
        }

        //events
        private void itemCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        //getters and setters
        public List<string> AllItemNames
        {
            get { return allItemNames; }
            set
            {
                allItemNames = value;
                OnPropertyChanged("AllItemNames");
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
