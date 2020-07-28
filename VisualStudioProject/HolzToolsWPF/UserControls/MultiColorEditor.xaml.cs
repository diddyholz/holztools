using System;
using System.Collections.Generic;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HolzTools.UserControls
{
    /// <summary>
    /// Interaction logic for MultiColorEditor.xaml
    /// </summary>
    public partial class MultiColorEditor : UserControl, INotifyPropertyChanged
    {
        private bool madeChanges = false;

        private Color selectedColor = Color.FromRgb(200, 0, 0);

        private List<Color> selectedLedColors;

        public MultiColorEditor(List<Color> ledColors)
        {
            InitializeComponent();

            // create the button for every led
            for (int x = 0; x < ledColors.Count; x++)
            {
                Button ledBtn = new Button();
                ledBtn.SetResourceReference(Control.StyleProperty, "ledColorBtnStyle");
                ledBtn.Background = new SolidColorBrush(ledColors[x]);
                ledBtn.Content = (x + 1).ToString();
                ledBtn.Click += LedBtn_Click;

                ledWrapPanel.Children.Add(ledBtn);
            }

            selectedLedColors = ledColors;

            MainWindow.ActiveWindow.multiColorEditorBackgroundGrid.MouseUp += CancelBtn_Click;

            DataContext = this;
        }

        private void LedBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            btn.Background = new SolidColorBrush(SelectedColor);
            selectedLedColors[Convert.ToInt32(btn.Content) - 1] = SelectedColor;

            MadeChanges = true;
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

        // Events
        private void DefaultColorButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            SolidColorBrush color = (SolidColorBrush)btn.Background;
            SelectedColor = color.Color;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.ShowMultiColorEditorWindow = false;
            MainWindow.ActiveWindow.multiColorEditorViewbox.Child = null;

            MainWindow.ActiveWindow.multiColorEditorBackgroundGrid.MouseUp -= CancelBtn_Click;
        }

        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ActiveWindow.SelectedLedItem.LedColorList = selectedLedColors;
            MainWindow.ActiveWindow.ShowMultiColorEditorWindow = false;
            MainWindow.ActiveWindow.multiColorEditorViewbox.Child = null;

            MainWindow.ActiveWindow.multiColorEditorBackgroundGrid.MouseUp -= CancelBtn_Click;
        }

        // Properties
        public Color SelectedColor
        {
            get { return selectedColor; }
            set
            {
                //select the new button
                foreach (Button defaultColorBtn in FindVisualChildren<Button>(this))
                {
                    SolidColorBrush background = (SolidColorBrush)defaultColorBtn.Background;

                    if (background.Color == SelectedColor && background.Color != value)
                    {
                        defaultColorBtn.Tag = "NotSelected";
                    }
                    else if (background.Color == value)
                    {
                        defaultColorBtn.Tag = "Selected";
                    }
                }

                selectedColor = value;
                OnPropertyChanged("SelectedColor");
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
