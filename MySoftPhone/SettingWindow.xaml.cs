using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MySoftPhone
{
    /// <summary>
    /// SettingWindow.xaml interaction logic
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow(PhoneSetting setting)
        {
            InitializeComponent();
            this.DataContext = setting;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }
}
