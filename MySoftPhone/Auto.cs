using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MySoftPhone
{
    /// <summary>
    /// 自动接听设置
    /// </summary>
    public class AutoHangupSettingInfo
    {
        public AutoHangupSettingInfo(string tel)
        {
            TelNumber = tel;
        }
        /// <summary>
        /// 电话号码
        /// </summary>
        public string TelNumber { get; }

        /// <summary>
        /// 是否自动接听
        /// </summary>
        public bool AutoHangup { get; set; }

        /// <summary>
        /// 接听之后多少秒自动挂断
        /// </summary>
        public int HangupAfter { get; set; }
    }

    /// <summary>
    /// 自动呼叫设置
    /// </summary>
    public class AutoCallSettingInfo
    {
        public AutoCallSettingInfo(string tel)
        {
            TelNumber = tel;
        }

        /// <summary>
        /// 电话号码
        /// </summary>
        public string TelNumber { get; }

        /// <summary>
        /// 是否自动呼叫
        /// </summary>
        public bool AutoCall { get; set; }

        /// <summary>
        /// 呼叫的目标号码
        /// </summary>
        public string CallTo { get; set; }

        /// <summary>
        /// 挂断后多少秒自动发起呼叫
        /// </summary>
        public int CallDelyAfterHangup { get; set; }
    }
}
