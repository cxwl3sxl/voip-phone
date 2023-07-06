using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MySoftPhone
{
    /// <summary>
    /// Auto Answer Settings
    /// </summary>
    public class AutoHangupSettingInfo
    {
        public AutoHangupSettingInfo(string tel)
        {
            TelNumber = tel;
        }
        /// <summary>
        /// telephone number
        /// </summary>
        public string TelNumber { get; }

        /// <summary>
        /// Whether to answer automatically
        /// </summary>
        public bool AutoHangup { get; set; }

        /// <summary>
        /// How many seconds to automatically hang up after answering
        /// </summary>
        public int HangupAfter { get; set; }
    }

    /// <summary>
    /// Auto Call Settings
    /// </summary>
    public class AutoCallSettingInfo
    {
        public AutoCallSettingInfo(string tel)
        {
            TelNumber = tel;
        }

        /// <summary>
        /// telephone number
        /// </summary>
        public string TelNumber { get; }

        /// <summary>
        /// Whether to call automatically
        /// </summary>
        public bool AutoCall { get; set; }

        /// <summary>
        /// Destination number to call
        /// </summary>
        public string CallTo { get; set; }

        /// <summary>
        /// How many seconds to automatically initiate a call after hanging up
        /// </summary>
        public int CallDelyAfterHangup { get; set; }
    }
}
