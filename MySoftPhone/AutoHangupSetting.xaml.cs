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
    /// AutoHangupSetting.xaml 的交互逻辑
    /// </summary>
    public partial class AutoHangupSetting : Window
    {
        public AutoHangupSetting()
        {
            InitializeComponent();
        }

        public IEnumerable<AutoHangupSettingInfo> Settings { get; set; }

        private void AutoHangupSetting_OnLoaded(object sender, RoutedEventArgs e)
        {
            DataGrid.ItemsSource = Settings;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
