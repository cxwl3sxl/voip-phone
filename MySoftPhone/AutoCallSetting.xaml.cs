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
    /// AutoCallSetting.xaml 的交互逻辑
    /// </summary>
    public partial class AutoCallSetting : Window
    {
        public AutoCallSetting()
        {
            InitializeComponent();
        }

        public IEnumerable<AutoCallSettingInfo> Settings { get; set; }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AutoCallSetting_OnLoaded(object sender, RoutedEventArgs e)
        {
            DataGrid.ItemsSource = Settings;
        }
    }
}
