using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using MySoftPhone.RPC;

namespace MySoftPhone
{
    public enum PhoneState
    {
        Ringing,
        InCall,
        CallEnded,
        Other,
        Unavailable,
        Available
    }

    public class PhoneProxy
    {
        public void Start()
        {
            _subProcess?.Stop();
            _subProcess = new SubProcess(new ProcessStartInfo(Assembly.GetEntryAssembly().Location,
                $"{_number} {_pwd} {_ip} {_port}"));
            _subProcess.MessageReceived += _subProcess_MessageReceived;
            _subProcess.SubProcessExited += _subProcess_SubProcessExited;
            _subProcess.Start();
            StateChanged?.Invoke(PhoneState.Available);
        }

        public bool PownOn { get; set; }

        private void _subProcess_SubProcessExited(SubProcess obj)
        {
            StateChanged?.Invoke(PhoneState.Unavailable);
            if (PownOn)
                Start();
        }

        private void _subProcess_MessageReceived(SubProcess arg1, string arg2)
        {
            var msgType = arg2.Substring(0, 3);
            switch (msgType)
            {
                case "$E:":
                    ProcessEvent(arg2.Substring(3));
                    break;
                case "$C:":
                    ProcessCall(arg2.Substring(3));
                    break;
                case "$R:":
                    ProcessReturn(arg2.Substring(3));
                    break;
            }
        }

        public void Stop()
        {
            _subProcess?.Stop();
            StateChanged?.Invoke(PhoneState.Unavailable);
        }

        private readonly string _number;
        private readonly string _pwd;
        private readonly string _ip;
        private readonly int _port;
        private SubProcess _subProcess;
        private readonly List<RemoteInvoke> _remoteInvokes = new List<RemoteInvoke>();
        private readonly object _remoteCallLocker = new object();

        public PhoneProxy(string number, string password, string ip, int port)
        {
            if (string.IsNullOrWhiteSpace(number)) throw new ArgumentNullException(nameof(number));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrWhiteSpace(ip)) throw new ArgumentNullException(nameof(ip));
            if (port <= 0) throw new ArgumentNullException(nameof(port));

            _number = number;
            _pwd = password;
            _ip = ip;
            _port = port;
        }

        void ProcessCall(string callInfo)
        {
            throw new NotSupportedException();
        }

        void ProcessEvent(string eventInfo)
        {
            if (string.IsNullOrWhiteSpace(eventInfo)) return;
            var info = eventInfo.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            switch (info[0])
            {
                case "RaiseMessage":
                    RaiseMessage?.Invoke(info[1]);
                    break;
                case "StateMessageChanged":
                    StateMessageChanged?.Invoke(info[1]);
                    break;
                case "StateChanged":
                    StateChanged?.Invoke((PhoneState)Enum.Parse(typeof(PhoneState), info[1]));
                    break;
            }
        }

        void ProcessReturn(string returnInfo)
        {
            var info = returnInfo.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            var id = info[0];
            var v = info[1];
            RemoteInvoke remoteCall = null;
            lock (_remoteCallLocker)
            {
                remoteCall = _remoteInvokes.FirstOrDefault(c => c.CallId == id);
                if (remoteCall != null)
                    _remoteInvokes.Remove(remoteCall);
            }
            remoteCall?.SetReasult(v);
            remoteCall?.Dispose();
        }

        /// <summary>
        /// 发送按键信号
        /// </summary>
        /// <param name="signal"></param>
        public void SendSignal(int signal)
        {
            CallRemote($"SendSignal#{signal}");
        }

        /// <summary>
        /// 停止发送信号
        /// </summary>
        public void StopSignal()
        {
            CallRemote($"StopSignal");
        }

        /// <summary>
        /// 拨号或者接听
        /// </summary>
        /// <param name="number"></param>
        public void PickUp(string number)
        {
            CallRemote($"PickUp#{number}");
        }

        /// <summary>
        /// 挂断
        /// </summary>
        public void HangUp()
        {
            CallRemote($"HangUp");
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

        public bool HaveCall => CallAndWait<bool>("HaveCall", s => Convert.ToBoolean(s));

        public bool IsInCall => CallAndWait<bool>("IsInCall", s => Convert.ToBoolean(s));

        void CallRemote(string callInfo)
        {
            _subProcess.SendMessage($"$C:{Guid.NewGuid().ToString()}#{callInfo}");
        }

        T CallAndWait<T>(string callInfo, Func<string, object> convert)
        {
            var remoteCall = new RemoteInvoke(convert);
            lock (_remoteCallLocker)
            {
                _remoteInvokes.Add(remoteCall);
            }
            _subProcess.SendMessage($"$C:{remoteCall.CallId}#{callInfo}");
            if (!remoteCall.Wait())
                throw new TimeoutException();
            return (T)remoteCall.Reasult;
        }
    }

    class RemoteInvoke : IDisposable
    {
        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        private readonly Func<string, object> _convert;
        public RemoteInvoke(Func<string, object> convert)
        {
            CallId = Guid.NewGuid().ToString();
            _convert = convert;
        }

        public string CallId { get; }

        public void SetReasult(string reasult)
        {
            Reasult = _convert(reasult);
            _autoResetEvent.Set();
        }

        public object Reasult { get; private set; }

        public bool Wait()
        {
            return _autoResetEvent.WaitOne(5000);
        }

        public void Dispose()
        {
            _autoResetEvent.Dispose();
        }
    }
}
