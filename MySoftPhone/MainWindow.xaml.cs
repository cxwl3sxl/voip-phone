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
    /// MainWindow.xaml interaction logic
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string PhoneSettingFile = "Phones.json";

        private AppSetting _appSetting = null;

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
                    _appSetting = JsonConvert.DeserializeObject<AppSetting>(
                        File.ReadAllText(PhoneSettingFile));
                }
                catch
                {
                    _appSetting = new AppSetting();
                }
            }
            else
            {
                _appSetting = new AppSetting();
            }
            foreach (var phone in _appSetting.Phones)
            {
                var p = new Phone()
                {
                    Setting = phone
                };
                p.OnRemove += P_OnRemove;
                Phones.Children.Add(p);
            }
            ReSizeWindow();
            if (_appSetting.Size.Width > 0 && _appSetting.Size.Height > 0)
            {
                Width = _appSetting.Size.Width;
                Height = _appSetting.Size.Height;
            }
            if (_appSetting.Location.X > 0 && _appSetting.Location.Y > 0)
            {
                Left = _appSetting.Location.X;
                Top = _appSetting.Location.Y;
            }
        }

        void ReSizeWindow()
        {
            if (_appSetting == null) return;
            if (_appSetting.Phones.Count <= 0)
            {
                Width = 217 + 20;
                Height = 370;
            }
            else if (_appSetting.Phones.Count <= 4)
            {
                Width = (217 * _appSetting.Phones.Count) + 20;
                Height = 370;
            }
            else if (_appSetting.Phones.Count == 4)
            {
                Width = 890;
                Height = 370;
            }
            else
            {
                Width = 905;
                Height = 405;
            }
        }

        private void P_OnRemove(Phone obj)
        {
            if (MessageBox.Show("Are you sure you want to remove the selected phone? ", "query", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                MessageBoxResult.Yes)
            {
                obj.Dispose();
                _appSetting.Phones.Remove(obj.Setting);
                Phones.Children.Remove(obj);
                ReSizeWindow();
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
                _appSetting.Phones.Add(setting);
                p.OnRemove += P_OnRemove;
                Phones.Children.Add(p);
            }
            ReSizeWindow();
        }

        private void Clear_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to remove all calls? ", "query", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                MessageBoxResult.Yes)
            {
                foreach (var phone in Phones.Children)
                {
                    if (phone is Phone p)
                    {
                        p.Dispose();
                    }
                }
                _appSetting.Phones.Clear();
                Phones.Children.Clear();
                ReSizeWindow();
            }
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to exit? ", "query", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
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
                _appSetting.Location = new Point(Left, Top);
                _appSetting.Size = new Size(Width, Height);
                File.WriteAllText(PhoneSettingFile, JsonConvert.SerializeObject(_appSetting, Formatting.Indented));
            }
            catch
            {
            }
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            new AboutWindow() { Owner = this }.ShowDialog();
        }

        private void License_OnClick(object sender, RoutedEventArgs e)
        {
            new LicenseWindow() { Owner = this }.ShowDialog();
        }

        private AutoHangupSettingInfo[] _autoHangupSetting = null;
        private void AutoHangup_OnClick(object sender, RoutedEventArgs e)
        {
            _autoCallSettingInfos = null;
            AutoHangup.IsChecked = true;
            AutoCall.IsChecked = false;
            if (_autoHangupSetting == null || _autoHangupSetting.Length != _appSetting.Phones.Count)
            {
                _autoHangupSetting = new AutoHangupSettingInfo[_appSetting.Phones.Count];
                for (var i = 0; i < _appSetting.Phones.Count; i++)
                {
                    _autoHangupSetting[i] = new AutoHangupSettingInfo(_appSetting.Phones[i].Number);
                }
            }
            new AutoHangupSetting() { Settings = _autoHangupSetting }.ShowDialog();
            foreach (var phone in Phones.Children)
            {
                if (!(phone is Phone p)) continue;
                var newAutoOperator = _autoHangupSetting.FirstOrDefault(s => s.TelNumber == p.Setting?.Number);
                p.SetAutoOperator(newAutoOperator);
            }
        }

        private AutoCallSettingInfo[] _autoCallSettingInfos = null;
        private void AutoCall_OnClick(object sender, RoutedEventArgs e)
        {
            _autoHangupSetting = null;
            AutoHangup.IsChecked = false;
            AutoCall.IsChecked = true;
            if (_autoCallSettingInfos == null || _autoCallSettingInfos.Length != _appSetting.Phones.Count)
            {
                _autoCallSettingInfos = new AutoCallSettingInfo[_appSetting.Phones.Count];
                for (var i = 0; i < _appSetting.Phones.Count; i++)
                {
                    _autoCallSettingInfos[i] = new AutoCallSettingInfo(_appSetting.Phones[i].Number);
                }
            }
            new AutoCallSetting { Settings = _autoCallSettingInfos }.ShowDialog();
            foreach (var phone in Phones.Children)
            {
                if (!(phone is Phone p)) continue;
                var newAutoOperator = _autoCallSettingInfos.FirstOrDefault(s => s.TelNumber == p.Setting?.Number);
                p.SetAutoOperator(newAutoOperator);
            }
        }
    }
}
