using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace MySoftPhone
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string PhoneSettingFile = "Phones.json";

        private List<PhoneSetting> _phones = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(PhoneSettingFile))
            {
                try
                {
                    _phones = JsonConvert.DeserializeObject<List<PhoneSetting>>(
                        File.ReadAllText(PhoneSettingFile));
                }
                catch
                {
                    _phones = new List<PhoneSetting>();
                }
            }
            else
            {
                _phones = new List<PhoneSetting>();
            }
            foreach (var phone in _phones)
            {
                var p = new Phone()
                {
                    Setting = phone
                };
                p.OnRemove += P_OnRemove;
                Phones.Children.Add(p);
            }
        }

        private void P_OnRemove(Phone obj)
        {
            if (MessageBox.Show("确定要移除选中的电话么？", "询问", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                MessageBoxResult.Yes)
            {
                obj.Dispose();
                _phones.Remove(obj.Setting);
                Phones.Children.Remove(obj);
            }
        }

        private void AddNew_OnClick(object sender, RoutedEventArgs e)
        {
            var setting = new PhoneSetting();
            SettingWindow window = new SettingWindow(setting);
            var r = window.ShowDialog();
            if (r.HasValue && r.Value)
            {
                var p = new Phone()
                {
                    Setting = setting
                };
                _phones.Add(setting);
                p.OnRemove += P_OnRemove;
                Phones.Children.Add(p);
            }
        }

        private void Clear_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要移除所有电话么？", "询问", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                MessageBoxResult.Yes)
            {
                foreach (var phone in Phones.Children)
                {
                    if (phone is Phone p)
                    {
                        p.Dispose();
                    }
                }
                _phones.Clear();
                Phones.Children.Clear();
            }
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要退出么？", "询问", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                MessageBoxResult.Yes)
            {
                foreach (var phone in Phones.Children)
                {
                    if (phone is Phone p)
                    {
                        p.Dispose();
                    }
                }
                Application.Current.Shutdown();
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            try
            {
                File.WriteAllText(PhoneSettingFile, JsonConvert.SerializeObject(_phones));
            }
            catch
            {
            }
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            new AboutWindow() { Owner = this }.ShowDialog();
        }
    }
}
