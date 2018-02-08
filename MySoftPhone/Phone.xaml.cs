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
            Disable();
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
            if (LabelNumber.Content != null && LabelNumber.Content.ToString() == Setting.Number)
            {
                LabelNumber.Content = "";
                return;
            }
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
                Disable();
            }
            else
            {
                CheckBoxPower.IsChecked = Setting.TurnOn;
                if (!Setting.TurnOn) return;
                try
                {
                    Dispose();
                    LabelClientInfo.Text = !string.IsNullOrWhiteSpace(Setting.Name) ? $"{Setting.Name}({Setting.Number})" : Setting.Number;
                    LabelClientServer.Text = Setting.ServerIp;
                    _phoneProxy = new PhoneProxy(Setting.Number, Setting.Password, Setting.ServerIp, Setting.Port);
                    _phoneProxy.PownOn = true;
                    _phoneProxy.RaiseMessage += _phoneProxy_RaiseMessage;
                    _phoneProxy.StateChanged += _phoneProxy_StateChanged;
                    _phoneProxy.StateMessageChanged += _phoneProxy_StateMessageChanged;
                    _phoneProxy.Start();
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
                if (obj == "Online")
                    Enable();
                if (obj == "Error")
                {
                    CheckBoxPower.IsChecked = false;
                    Setting.TurnOn = false;
                }
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
                    case PhoneState.Available:
                        break;
                    case PhoneState.Unavailable:
                        Disable();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
                }
            });
        }

        void Disable()
        {
            LabelStatus.Content = "离线";
            foreach (var child in GridButtons.Children)
            {
                if (child is Button btn)
                {
                    btn.IsEnabled = false;
                }
            }
            btnSetting.IsEnabled = true;
        }

        void Enable()
        {
            foreach (var child in GridButtons.Children)
            {
                if (child is Button btn)
                {
                    btn.IsEnabled = true;
                }
            }
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

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (Setting.TurnOn)
            {
                if (MessageBox.Show("确定要关闭该电话么？", "询问", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                    MessageBoxResult.No)
                {
                    Setting.TurnOn = true;
                    CheckBoxPower.IsChecked = true;
                }
                else
                {
                    Setting.TurnOn = false;
                    _phoneProxy.PownOn = false;
                    _phoneProxy.Stop();
                }
            }
            else
            {
                Setting.TurnOn = true;
                InitPhone();
            }
        }
    }
}
