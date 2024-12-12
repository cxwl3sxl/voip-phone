using Ozeki.VoIP;
using Ozeki.VoIP.SDK;
using System;
using System.IO;
using System.Net;
using Ozeki.Media.MediaHandlers;
using Ozeki.Network.Nat;

namespace MySoftPhone
{
    public class RealPhone : IDisposable
    {
        ISoftPhone softPhone;
        IPhoneLine phoneLine;
        IPhoneCall call;
        private Microphone microphone = Microphone.GetDefaultDevice();
        private Speaker speaker = Speaker.GetDefaultDevice();
        MediaConnector connector = new MediaConnector();
        PhoneCallAudioSender mediaSender = new PhoneCallAudioSender();
        PhoneCallAudioReceiver mediaReceiver = new PhoneCallAudioReceiver();
        int currentDtmfSignal;
        private readonly Logger _logger;

        public RealPhone(string localIp, string number, string password, string ip, int port)
        {
            _logger = new Logger($"MyPhone-{number}");
            if (File.Exists("license.txt"))
            {
                string[] licenseInfo = File.ReadAllLines("license.txt");
                var uid = licenseInfo.Length > 0 ? licenseInfo[0] : "";
                var pwd = licenseInfo.Length > 1 ? licenseInfo[1] : "";
                //http://geekwolke.com/2016/10/04/ozeki-voip-sip-sdk-license-code-free-download/
                Ozeki.VoIP.SDK.Protection.LicenseManager.Instance.SetLicense(uid, pwd);
            }

            softPhone = SoftPhoneFactory.CreateSoftPhone(IPAddress.Parse(localIp), 15000, 15500);
            softPhone.IncomingCall += softPhone_IncomingCall;

            var lineConfig =
                new PhoneLineConfiguration(new SIPAccount(true, number, number, number, password, ip, port))
                {
                    NatConfig = new NatConfiguration(NatTraversalMethod.None),
                    LocalAddress = localIp,
                    
                };
            phoneLine = softPhone.CreatePhoneLine(lineConfig);
            phoneLine.RegistrationStateChanged += phoneLine_PhoneLineStateChanged;

            softPhone.RegisterPhoneLine(phoneLine);

            ConnectMedia();
        }

        private void phoneLine_PhoneLineStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            _logger.Information($"线路状态变更事件 {e.State} {e.ReasonPhrase}");
            StateMessageChanged?.Invoke(e.State == RegState.RegistrationSucceeded
                ? "Online"
                : $"{e.State} {e.ReasonPhrase}");

            if (e.State == RegState.RegistrationSucceeded)
            {
                _logger.Information($"线路已注册 {phoneLine.Config.LocalAddress} {phoneLine.Config.LocalPort}");
            }
        }

        private void softPhone_IncomingCall(object sender, VoIPEventArgs<IPhoneCall> e)
        {
            try
            {
                _logger.Information(
                    $"收到呼入请求，来电号码：{e.Item.DialInfo.CallerDisplay}");
                var incomingCall = e.Item as IPhoneCall;
                if (call != null)
                {
                    incomingCall.Reject();
                    _logger.Information($"当前正忙，直接拒接");
                    return;
                }

                call = incomingCall;
                SubscribeToCallEvents(call);

                StateChanged?.Invoke(PhoneState.InCall);
                RaiseMessage?.Invoke(String.Format("Incoming call from {0}", e.Item.DialInfo.CallerDisplay));
            }
            catch (Exception ex)
            {
                _logger.Error("处理来电出错", ex);
            }
        }

        /// <summary>
        /// 发送按键信号
        /// </summary>
        /// <param name="signal"></param>
        public void SendSignal(int signal)
        {
            if (call == null)
                return;

            if (!call.CallState.IsInCall())
                return;

            if (signal == -1)
                return;

            currentDtmfSignal = signal;
            call.StartDTMFSignal((DtmfNamedEvents)signal);
        }

        /// <summary>
        /// 停止发送信号
        /// </summary>
        public void StopSignal()
        {
            if (call == null)
                return;

            if (!call.CallState.IsInCall())
                return;

            call.StopDTMFSignal((DtmfNamedEvents)currentDtmfSignal);
        }

        /// <summary>
        /// 拨号或者接听
        /// </summary>
        /// <param name="number"></param>
        public void PickUp(string number)
        {
            // accept incoming call
            if (call != null && call.IsIncoming)
            {
                _logger.Information($"正在接起来电：{call.DialInfo.CallerDisplay}");
                RaiseMessage?.Invoke("Talking");
                call.Answer();
                return;
            }

            // dial
            if (call != null)
                return;

            if (string.IsNullOrEmpty(number))
                return;

            if (phoneLine.RegState != RegState.RegistrationSucceeded)
            {
                RaiseMessage?.Invoke("Phone line must be registered!");
                return;
            }

            _logger.Information($"正在呼叫：{number}");
            call = softPhone.CreateCallObject(phoneLine, number);
            SubscribeToCallEvents(call);
            call.Start();
            RaiseMessage?.Invoke($"Calling {number}");
        }

        private void SubscribeToCallEvents(IPhoneCall call)
        {
            if (call == null)
                return;

            call.CallStateChanged += (call_CallStateChanged);
            call.DtmfReceived += (call_DtmfReceived);
            call.TransferStateChanged += Call_TransferStateChanged;
            call.InstantMessageReceived += Call_InstantMessageReceived;
        }

        private void UnsubscribeFromCallEvents(IPhoneCall call)
        {
            if (call == null)
                return;

            call.CallStateChanged -= (call_CallStateChanged);
            call.DtmfReceived -= (call_DtmfReceived);
            call.TransferStateChanged -= Call_TransferStateChanged;
            call.InstantMessageReceived -= Call_InstantMessageReceived;
        }

        private void Call_InstantMessageReceived(object sender, Ozeki.VoIP.SIP.InstantMessage e)
        {
            _logger.Information($"收到消息 {e.Content}");
        }

        private void Call_TransferStateChanged(object sender, VoIPEventArgs<TransferState> e)
        {
            _logger.Information($"通话传输状态变更 {e.Item}");
        }

        private void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            _logger.Information($"呼叫状态变更事件：{e.State} {e.Reason}");
            switch (e.State)
            {
                case CallState.Ringing:
                    StateChanged?.Invoke(PhoneState.Ringing);
                    break;
                case CallState.InCall:
                    StateChanged?.Invoke(PhoneState.InCall);
                    break;
                default:
                    StateChanged?.Invoke(PhoneState.Other);
                    break;
            }
            RaiseMessage?.Invoke(e.State.ToString());

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
                StateChanged?.Invoke(PhoneState.CallEnded);
            }
        }

        private void ConnectMedia()
        {
            if (speaker != null)
                connector.Connect(mediaReceiver, speaker);

            if (microphone != null)
                connector.Connect(microphone, mediaSender);
        }

        private void call_DtmfReceived(object sender, VoIPEventArgs<DtmfInfo> e)
        {
            DtmfSignal signal = e.Item.Signal;
            RaiseMessage?.Invoke(String.Format("DTMF signal received: {0} ", signal.Signal));
        }

        /// <summary>
        /// 挂断
        /// </summary>
        public void HangUp()
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
        }

        public void Dispose()
        {
            phoneLine?.Dispose();
            softPhone?.Close();
        }

        /// <summary>
        /// 电话状态发生变化
        /// </summary>
        public event Action<PhoneState> StateChanged;

        /// <summary>
        /// 推送消息
        /// </summary>
        public event Action<string> RaiseMessage;

        /// <summary>
        /// 状态发生变化的消息
        /// </summary>
        public event Action<string> StateMessageChanged;

        public bool HaveCall => call != null;
        public bool IsInCall => call != null && call.CallState.IsInCall();
    }
}
