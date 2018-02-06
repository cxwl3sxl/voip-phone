using Ozeki.VoIP;
using Ozeki.VoIP.SDK;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ozeki.Media.MediaHandlers;

namespace MySoftPhone
{
    /// <summary>
    /// Phone.xaml 的交互逻辑
    /// </summary>
    public partial class Phone : UserControl, IDisposable
    {
        public event Action<Phone> OnRemove;

        ISoftPhone softPhone;
        IPhoneLine phoneLine;
        IPhoneCall call;
        private Microphone microphone = Microphone.GetDefaultDevice();
        private Speaker speaker = Speaker.GetDefaultDevice();
        MediaConnector connector = new MediaConnector();
        PhoneCallAudioSender mediaSender = new PhoneCallAudioSender();
        PhoneCallAudioReceiver mediaReceiver = new PhoneCallAudioReceiver();
        int currentDtmfSignal;

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
            if (call != null)
            {
                if (call.IsIncoming && call.CallState.IsRinging())
                {
                    call.Reject();
                }
                else
                {
                    call.HangUp();
                }

                call = null;
            }
            LabelNumber.Content = string.Empty;
        }

        private void buttonKeyPadButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null)
                return;

            if (call != null)
                return;

            LabelNumber.Content += btn.Content.ToString().Trim();
        }

        private void buttonKeyPad_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (call == null)
                return;

            if (!call.CallState.IsInCall())
                return;

            var btn = sender as Button;
            int dtmfSignal = GetDtmfSignalFromButtonTag(btn);
            if (dtmfSignal == -1)
                return;

            currentDtmfSignal = dtmfSignal;
            call.StartDTMFSignal((DtmfNamedEvents)dtmfSignal);
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
            // accept incoming call
            if (call != null && call.IsIncoming)
            {
                call.Answer();
                return;
            }

            // dial
            if (call != null)
                return;

            if (string.IsNullOrEmpty(LabelNumber.Content?.ToString()))
                return;

            if (phoneLine.RegState != RegState.RegistrationSucceeded)
            {
                MessageBox.Show("Phone line must be registered!");
                return;
            }

            call = softPhone.CreateCallObject(phoneLine, LabelNumber.Content.ToString());
            SubscribeToCallEvents(call);
            call.Start();
        }

        private void SubscribeToCallEvents(IPhoneCall call)
        {
            if (call == null)
                return;

            call.CallStateChanged += (call_CallStateChanged);
            call.DtmfReceived += (call_DtmfReceived);
        }

        private void InvokeGUIThread(Action action)
        {
            Dispatcher.Invoke(action, null);
        }

        private void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            InvokeGUIThread(() =>
            {
                LabelMessage.Content = e.State.ToString();
                switch (e.State)
                {
                    case CallState.Ringing:
                        LabelMessage.Background = new SolidColorBrush(Colors.Red);
                        break;
                    case CallState.InCall:
                        LabelMessage.Background = new SolidColorBrush(Colors.Green);
                        break;
                    default:
                        LabelMessage.Background = new SolidColorBrush(Colors.White);
                        break;
                }
            });

            if (e.State == CallState.Answered)
            {
                if (microphone != null)
                    microphone.Start();

                if (speaker != null)
                    speaker.Start();

                mediaSender.AttachToCall(call);
                mediaReceiver.AttachToCall(call);
                return;
            }

            if (e.State.IsCallEnded())
            {
                if (microphone != null)
                    microphone.Stop();

                if (speaker != null)
                    speaker.Stop();

                mediaSender.Detach();
                mediaReceiver.Detach();

                UnsubscribeFromCallEvents(sender as IPhoneCall);
                call = null;

                InvokeGUIThread(() => { LabelNumber.Content = string.Empty; });
            }
        }

        private void UnsubscribeFromCallEvents(IPhoneCall call)
        {
            if (call == null)
                return;

            call.CallStateChanged -= (call_CallStateChanged);
            call.DtmfReceived -= (call_DtmfReceived);
        }

        private void call_DtmfReceived(object sender, VoIPEventArgs<DtmfInfo> e)
        {
            DtmfSignal signal = e.Item.Signal;
            InvokeGUIThread(() => LabelMessage.Content = String.Format("DTMF signal received: {0} ", signal.Signal));
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
            //try
            //{
            //    softPhone = SoftPhoneFactory.CreateSoftPhone(15000, 15500);
            //    softPhone.IncomingCall += softPhone_IncomingCall;
            //    phoneLine = softPhone.CreatePhoneLine(new SIPAccount(true, "1001", "1001", "1001", "12345", "192.168.0.33", 5060));
            //    phoneLine.RegistrationStateChanged += phoneLine_PhoneLineStateChanged;

            //    softPhone.RegisterPhoneLine(phoneLine);

            //    ConnectMedia();
            //}
            //catch (Exception ex)
            //{
            //    var sb = new StringBuilder();
            //    sb.AppendLine("Error while initializing softphone.");
            //    sb.AppendLine();
            //    sb.AppendLine("Exception:");
            //    sb.AppendLine(ex.Message);
            //    sb.AppendLine();
            //    if (ex.InnerException != null)
            //    {
            //        sb.AppendLine("Inner Exception:");
            //        sb.AppendLine(ex.InnerException.Message);
            //        sb.AppendLine();
            //    }
            //    sb.AppendLine("StackTrace:");
            //    sb.AppendLine(ex.StackTrace);

            //    MessageBox.Show(String.Format("{0}", sb), "Ozeki WPF Softphone Sample", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
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
                    softPhone = SoftPhoneFactory.CreateSoftPhone(15000, 15500);
                    softPhone.IncomingCall += softPhone_IncomingCall;
                    phoneLine = softPhone.CreatePhoneLine(new SIPAccount(true, Setting.Number, Setting.Number, Setting.Number, Setting.Password, Setting.ServerIp, Setting.Port));
                    phoneLine.RegistrationStateChanged += phoneLine_PhoneLineStateChanged;

                    softPhone.RegisterPhoneLine(phoneLine);

                    ConnectMedia();
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

        public PhoneSetting Setting { get; set; }



        private void ConnectMedia()
        {
            if (speaker != null)
                connector.Connect(mediaReceiver, speaker);

            if (microphone != null)
                connector.Connect(microphone, mediaSender);
        }

        private void buttonKeyPad_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (call == null)
                return;

            if (!call.CallState.IsInCall())
                return;

            call.StopDTMFSignal((DtmfNamedEvents)currentDtmfSignal);
        }

        private void phoneLine_PhoneLineStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            InvokeGUIThread(() =>
            {
                var account = ((IPhoneLine)sender).SIPAccount;
                LabelClientInfo.Text = account.RegisterName;
                LabelClientServer.Text = account.DomainServerHost;
                if (e.State == RegState.RegistrationSucceeded)
                {
                    LabelStatus.Content = "Online";
                }
                else
                    LabelStatus.Content = e.State.ToString();
            });
        }

        private void softPhone_IncomingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            var incomingCall = e.Item as IPhoneCall;
            if (call != null)
            {
                incomingCall.Reject();
                return;
            }

            call = incomingCall;
            SubscribeToCallEvents(call);

            InvokeGUIThread(() =>
            {
                LabelMessage.Content = String.Format("Incoming call from {0}", e.Item.DialInfo.CallerDisplay);
                LabelMessage.Background = new SolidColorBrush(Colors.Red);
            });
        }

        public void Dispose()
        {
            call?.HangUp();
            phoneLine?.Dispose();
            softPhone?.Close();
        }
    }
}
