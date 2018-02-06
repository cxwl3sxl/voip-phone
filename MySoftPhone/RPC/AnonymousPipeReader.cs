using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace MySoftPhone.RPC
{
    public class AnonymousPipeReader
    {
        private readonly AnonymousPipeServerStream _anonymousPipeServerStream;
        private readonly StreamReader _pipeReader;
        private bool _stop;

        public event Action<string> MessageReceived;

        public AnonymousPipeReader(AnonymousPipeServerStream pipeServerStream)
        {
            _anonymousPipeServerStream = pipeServerStream ?? throw new ArgumentNullException(nameof(pipeServerStream));
            _pipeReader = new StreamReader(_anonymousPipeServerStream);
        }

        public void Start()
        {
            Task.Factory.StartNew(ReadMessage);
        }

        public void Stop()
        {
            _stop = true;
        }

        void ReadMessage()
        {
            while (!_stop)
            {
                var msg = _pipeReader?.ReadLine();
                if (!string.IsNullOrWhiteSpace(msg))
                    MessageReceived?.BeginInvoke(msg, null, null);
                Thread.Sleep(500);
            }
        }
    }
}
