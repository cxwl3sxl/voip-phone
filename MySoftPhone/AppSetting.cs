using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MySoftPhone
{
    public class AppSetting
    {
        public AppSetting()
        {
            Phones = new List<PhoneSetting>();
        }

        public Point Location { get; set; }
        public Size Size { get; set; }
        public List<PhoneSetting> Phones { get; set; }
    }
}
