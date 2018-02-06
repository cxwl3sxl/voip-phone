using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MySoftPhone
{
    /// <summary>
    /// Phone.xaml 的交互逻辑
    /// </summary>
    public partial class Phone : UserControl, IDisposable
    {
        public event Action<Phone> OnRemove;
        private PhoneProxy _phoneProxy;

        public Phone()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OnRemove?.Invoke(this);
        }

        private void buttonHangUp_Click(object sender, RoutedEventArgs e)
        {
            _phoneProxy?.HangUp();
            LabelNumber.Content = string.Empty;
        }

        private void buttonKeyPadButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null)
                return;

            if (_phoneProxy.HaveCall)
                return;

            LabelNumber.Content += btn.Content.ToString().Trim();
        }

        private void buttonKeyPad_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_phoneProxy.HaveCall)
                return;

            if (!_phoneProxy.IsInCall)
                return;

            var btn = sender as Button;
            int dtmfSignal = GetDtmfSignalFromButtonTag(btn);
            if (dtmfSignal == -1)
                return;

            _phoneProxy.SendSignal(dtmfSignal);
        }

        private int GetDtmfSignalFromButtonTag(Button button)
        {
            if (button == null)
                return -1;

            if (button.Tag == null)
                return -1;

            int signal;
            if (int.TryParse(button.Tag.ToString(), out signal))
                return signal;

            return -1;
        }

        private void buttonPickUp_Click(object sender, RoutedEventArgs e)
        {
            _phoneProxy.PickUp(LabelNumber.Content?.ToString());
        }

        private void InvokeGUIThread(Action action)
        {
            Dispatcher.Invoke(action, null);
        }

        private void Setting_OnClick(object sender, RoutedEventArgs e)
        {
            var phoneSetting = Setting ?? new PhoneSetting();
            SettingWindow win = new SettingWindow(phoneSetting);
            var r = win.ShowDialog();
            if (r.HasValue && r.Value)
            {
                Setting = phoneSetting;
                InitPhone();
            }
        }

        private void Phone_OnLoaded(object sender, RoutedEventArgs e)
        {
            InitPhone();
        }

        void InitPhone()
        {
            if (Setting == null)
            {
                MessageBox.Show("电话尚未初始化！");
                foreach (var child in GridButtons.Children)
                {
                    if (child is Button btn)
                    {
                        btn.IsEnabled = false;
                    }
                }
                btnSetting.IsEnabled = true;
            }
            else
            {
                foreach (var child in GridButtons.Children)
                {
                    if (child is Button btn)
                    {
                        btn.IsEnabled = true;
                    }
                }
                try
                {
                    Dispose();
                    LabelClientInfo.Text = Setting.Number;
                    LabelClientServer.Text = Setting.ServerIp;
                    _phoneProxy = new PhoneProxy(Setting.Number, Setting.Password, Setting.ServerIp, Setting.Port);
                    _phoneProxy.RaiseMessage += _phoneProxy_RaiseMessage;
                    _phoneProxy.StateChanged += _phoneProxy_StateChanged;
                    _phoneProxy.StateMessageChanged += _phoneProxy_StateMessageChanged;
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Error while initializing softphone.");
                    sb.AppendLine();
                    sb.AppendLine("Exception:");
                    sb.AppendLine(ex.Message);
                    sb.AppendLine();
                    if (ex.InnerException != null)
                    {
                        sb.AppendLine("Inner Exception:");
                        sb.AppendLine(ex.InnerException.Message);
                        sb.AppendLine();
                    }
                    sb.AppendLine("StackTrace:");
                    sb.AppendLine(ex.StackTrace);

                    MessageBox.Show(String.Format("{0}", sb), "Ozeki WPF Softphone Sample", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void _phoneProxy_StateMessageChanged(string obj)
        {
            InvokeGUIThread(() =>
            {
                LabelStatus.Content = obj;
            });
        }

        private void _phoneProxy_StateChanged(PhoneState obj)
        {
            InvokeGUIThread(() =>
            {
                switch (obj)
                {
                    case PhoneState.Ringing:
                        LabelMessage.Background = new SolidColorBrush(Colors.Red);
                        break;
                    case PhoneState.InCall:
                        LabelMessage.Background = new SolidColorBrush(Colors.Green);
                        break;
                    case PhoneState.CallEnded:
                    case PhoneState.Other:
                        LabelMessage.Background = new SolidColorBrush(Colors.White);
                        LabelNumber.Content = string.Empty;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
                }
            });
        }

        private void _phoneProxy_RaiseMessage(string obj)
        {
            InvokeGUIThread(() =>
            {
                LabelMessage.Content = obj;
            });
        }

        public PhoneSetting Setting { get; set; }

        private void buttonKeyPad_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_phoneProxy.HaveCall)
                return;

            if (!_phoneProxy.IsInCall)
                return;

            _phoneProxy.StopSignal();
        }

        public void Dispose()
        {
            _phoneProxy?.Stop();
        }
    }
}
