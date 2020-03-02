using HolzTools.UserControls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace HolzTools.ModeControls
{
    public partial class ModeSync : INotifyPropertyChanged
    {
        private string syncedLedItem;

        private List<string> selectedItemSyncableItems = new List<string>();

        public ModeSync()
        {
            InitializeComponent();
            DataContext = this;
        }

        //events

        //getters and setters
        public List<string> SelectedItemSyncableItems
        {
            get { return selectedItemSyncableItems; }
            set
            {
                selectedItemSyncableItems = value;
                OnPropertyChanged("SelectedItemSyncableItems");

                if (selectedItemSyncableItems.Count != 0)
                    this.Dispatcher.Invoke(() => { syncableItemsCB.SelectedIndex = 0; });
            }
        }

        public string SyncedLedItem
        {
            get { return syncedLedItem; }
            set
            {
                syncedLedItem = value;
                OnPropertyChanged("SyncedLeditem");
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
