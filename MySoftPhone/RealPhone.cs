using Ozeki.VoIP;
using Ozeki.VoIP.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ozeki.Media.MediaHandlers;

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

        public RealPhone(string number, string password, string ip, int port)
        {
            //http://geekwolke.com/2016/10/04/ozeki-voip-sip-sdk-license-code-free-download/
            
            softPhone = SoftPhoneFactory.CreateSoftPhone(15000, 15500);
            softPhone.IncomingCall += softPhone_IncomingCall;
            phoneLine = softPhone.CreatePhoneLine(new SIPAccount(true, number, number, number, password, ip, port));
            phoneLine.RegistrationStateChanged += phoneLine_PhoneLineStateChanged;

            softPhone.RegisterPhoneLine(phoneLine);

            ConnectMedia();
        }

        private void phoneLine_PhoneLineStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            StateMessageChanged?.Invoke(e.State == RegState.RegistrationSucceeded ? "Online" : e.State.ToString());
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

            StateChanged?.Invoke(PhoneState.InCall);
            RaiseMessage?.Invoke(String.Format("Incoming call from {0}", e.Item.DialInfo.CallerDisplay));
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
        }

        private void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
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
