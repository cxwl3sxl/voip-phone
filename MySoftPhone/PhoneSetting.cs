using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySoftPhone
{
    public class PhoneSetting
    {
        public PhoneSetting()
        {
            Port = 5060;
            TurnOn = true;
        }

        public string Number { get; set; }
        public string Password { get; set; }
        public string ServerIp { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
        public bool TurnOn { get; set; }
    }
}
