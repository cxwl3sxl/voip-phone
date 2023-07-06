using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MySoftPhone
{
    /// <summary>
    /// Phone.xaml interaction logic
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
                MessageBox.Show("The phone has not been initialized!");
                Disable();
            }
            else
            {
                CheckBoxPower.IsChecked = Setting.TurnOn;
                if (!Setting.TurnOn) return;
                try
                {
                    Dispose();
                    Starting();
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

                    MessageBox.Show(String.Format("{0}", sb), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void _phoneProxy_StateMessageChanged(string obj)
        {
            InvokeGUIThread(() =>
            {
                LabelStatus.Content = obj;
                if (obj == "Online")
                {
                    Enable();
                    Started();
                }
                if (obj == "Error")
                {
                    CheckBoxPower.IsChecked = false;
                    Setting.TurnOn = false;
                    _phoneProxy?.Stop();
                    Started();
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
            LabelStatus.Content = "offline";
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
            Setting.TurnOn = true;
            CheckBoxPower.IsChecked = true;
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
                if (MessageBox.Show("Are you sure you want to close this phone? ", "query", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
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

        private void Starting()
        {
            ellipseStarting.Visibility = Visibility.Visible;
            CheckBoxPower.Visibility = Visibility.Collapsed;
            var beginStory = (Storyboard)FindResource("StoryboardStarting");
            beginStory?.Begin(ellipseStarting, true);
        }

        private void Started()
        {
            var beginStory = (Storyboard)FindResource("StoryboardStarting");
            beginStory?.Stop(ellipseStarting);
            ellipseStarting.Visibility = Visibility.Collapsed;
            CheckBoxPower.Visibility = Visibility.Visible;
        }

        public void SetAutoOperator(AutoHangupSettingInfo setting)
        {
            GridButtons.IsEnabled = true;
            if (setting == null) return;
            GridButtons.IsEnabled = !setting.AutoHangup;
            _autoHangupSetting = null;
            StopAuto();
            _autoHangupSetting = setting;
            StartAuto();
        }

        private AutoCallSettingInfo _autoCallSetting;
        private AutoHangupSettingInfo _autoHangupSetting;
        private AutoResetEvent _autoResetEvent;
        public void SetAutoOperator(AutoCallSettingInfo setting)
        {
            GridButtons.IsEnabled = true;
            if (setting == null) return;
            GridButtons.IsEnabled = !setting.AutoCall;
            _autoCallSetting = null;
            StopAuto();
            _autoCallSetting = setting;
            StartAuto();
        }

        void StopAuto()
        {
            _autoResetEvent?.Set();
            _autoResetEvent?.WaitOne(500);
            _autoResetEvent?.Dispose();
        }

        void StartAuto()
        {
            if (_autoCallSetting == null && _autoHangupSetting == null) return;
            _autoResetEvent = new AutoResetEvent(false);
            Task.Factory.StartNew(AutoOperator);
        }

        void AutoOperator()
        {
            while (true)
            {
                if (_autoCallSetting == null && _autoHangupSetting == null) break;
                if (_autoCallSetting != null)
                {
                    if (string.IsNullOrWhiteSpace(_autoCallSetting.CallTo) || !_autoCallSetting.AutoCall) break;
                    Dispatcher.Invoke(new Action(ProcessAutoCall));
                }
                if (_autoHangupSetting != null)
                {
                    if (!_autoHangupSetting.AutoHangup) break;
                    Dispatcher.Invoke(new Action(ProcessAutoHangup));
                }
                _autoResetEvent.WaitOne(1000); //Check every 1 second
            }
        }

        private DateTime _lastCallAt = DateTime.MinValue;

        void ProcessAutoCall()
        {
            //automatic call
            //_autoCallSetting
            //is calling
            if (LabelMessage.Content?.ToString() == "InCall")
            {
                _lastCallAt = DateTime.Now;
                return;
            }
            //The time interval after hanging up does not meet the configuration
            if (!((DateTime.Now - _lastCallAt).TotalSeconds >= _autoCallSetting.CallDelyAfterHangup)) return;
            //start calling
            _phoneProxy.PickUp(_autoCallSetting.CallTo);
            //Record last call time
            _lastCallAt = DateTime.Now;
        }

        private DateTime _lastPickUp;
        void ProcessAutoHangup()
        {
            //Auto answer, no ring
            if (LabelMessage.Content?.ToString() == "Ringing")
            {
                _phoneProxy.PickUp("");
                _lastPickUp = DateTime.Now;
            }
            if (LabelMessage.Content?.ToString() == "InCall" &&
                (DateTime.Now - _lastPickUp).TotalSeconds >= _autoHangupSetting.HangupAfter)
            {
                _phoneProxy.HangUp();
            }
        }
    }
}
