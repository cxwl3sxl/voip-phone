using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySoftPhone
{
    public enum PhoneState
    {
        Ringing,
        InCall,
        CallEnded,
        Other
    }

    public class PhoneProxy
    {
        public void Start()
        {
        }

        public void Stop()
        {
        }

        private readonly RealPhone _realPhone;
        public PhoneProxy(string number, string password, string ip, int port)
        {
            _realPhone = new RealPhone(number, password, ip, port);
            _realPhone.RaiseMessage += _realPhone_RaiseMessage;
            _realPhone.StateChanged += _realPhone_StateChanged;
            _realPhone.StateMessageChanged += _realPhone_StateMessageChanged;
        }

        private void _realPhone_StateMessageChanged(string obj)
        {
            StateMessageChanged?.Invoke(obj);
        }

        private void _realPhone_StateChanged(PhoneState obj)
        {
            StateChanged?.Invoke(obj);
        }

        private void _realPhone_RaiseMessage(string obj)
        {
            RaiseMessage?.Invoke(obj);
        }

        /// <summary>
        /// 发送按键信号
        /// </summary>
        /// <param name="signal"></param>
        public void SendSignal(int signal)
        {
            _realPhone.SendSignal(signal);
        }

        /// <summary>
        /// 停止发送信号
        /// </summary>
        public void StopSignal()
        {
            _realPhone.StopSignal();
        }

        /// <summary>
        /// 拨号或者接听
        /// </summary>
        /// <param name="number"></param>
        public void PickUp(string number)
        {
            _realPhone.PickUp(number);
        }

        /// <summary>
        /// 挂断
        /// </summary>
        public void HangUp()
        {
            _realPhone.HangUp();
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

        public bool HaveCall => _realPhone.HaveCall;
        public bool IsInCall => _realPhone.IsInCall;
    }
}
