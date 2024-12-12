using System;
using System.Diagnostics;

namespace MySoftPhone
{
    internal class Logger
    {
        private readonly string _name;

        public Logger(string name)
        {
            _name = name;
        }

        public void Warning(string message)
        {
            Trace.TraceWarning($"[{_name}] [W] {message}");
        }

        public void Error(string message, Exception ex = null)
        {
            Trace.TraceError($"[{_name}] [E] {message} {ex}");
        }

        public void Information(string message)
        {
            Trace.TraceInformation($"[{_name}] [I] {message}");
        }
    }
}
