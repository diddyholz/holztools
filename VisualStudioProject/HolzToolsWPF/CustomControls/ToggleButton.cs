using System;
using System.Collections.Generic;
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

namespace HolzTools.CustomControls
{
    public class ToggleButton : Control
    {
        public event EventHandler ToggledChanged;

        public static readonly DependencyProperty ToggledProperty = DependencyProperty.Register("Toggled", typeof(bool), typeof(ToggleButton), new PropertyMetadata(false));

        public static readonly DependencyProperty IsPressedProperty = DependencyProperty.Register("IsPressed", typeof(bool), typeof(ToggleButton), new PropertyMetadata(false));

        static ToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleButton), new FrameworkPropertyMetadata(typeof(ToggleButton)));
        }

        //events
        public void OnToggledChanged()
        {
            EventHandler handler = ToggledChanged;
            if (null != handler) handler(this, EventArgs.Empty);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Border b = this.Template.FindName("rootBorder", this) as Border;
            if (b != null)
            {
                b.MouseUp += B_MouseUp;
                b.MouseDown += B_MouseDown;
                b.MouseLeave += B_MouseLeave;
            }
        }

        private void B_MouseLeave(object sender, MouseEventArgs e)
        {
            IsPressed = false;
        }

        private void B_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsPressed = true;
        }

        private void B_MouseUp(object sender, RoutedEventArgs e)
        {
            IsPressed = false;
            Toggled = !Toggled;
        }

        //getters and setters
        public bool Toggled
        {
            get { return (bool)GetValue(ToggledProperty); }
            set
            {
                SetValue(ToggledProperty, value);
                OnToggledChanged();
            }
        }

        public bool IsPressed
        {
            get { return (bool)GetValue(IsPressedProperty); }
            set { SetValue(IsPressedProperty, value); }
        }

        public Color AccentColor
        {
            get { return MainWindow.ActiveWindow.AccentColor; }
        }
    }
}
