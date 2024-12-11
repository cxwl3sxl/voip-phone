using System.Linq;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace MySoftPhone
{
    public class PhoneSetting
    {
        private static string[] AllLocalIpAddress { get; }

        static PhoneSetting()
        {
            AllLocalIpAddress = Dns
                .GetHostAddresses(Dns.GetHostName())
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork && a.ToString() != "127.0.0.1")
                .Select(a => a.ToString())
                .ToArray();
        }

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
        public string LocalIp { get; set; }

        [JsonIgnore] public string[] AllLocalIp => AllLocalIpAddress;
    }
}
