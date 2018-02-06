using System;
using System.Collections.Concurrent;
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
    public class AnonymousPipeWriter
    {
        private readonly PipeStream _pipeClient;
        private bool _stop;
        private readonly ConcurrentQueue<string> _messageConcurrentQueue;
        private readonly AutoResetEvent _autoResetEvent;
        private int _parentProcessId;
        public event Action ParentProcessExit;

        public AnonymousPipeWriter(AnonymousPipeClientStream pipeClientStream, int parentProcessId = -1)
        {
            if (_pipeClient == null) throw new Exception("参数无效，无法和主进程通信！");
            _pipeClient.ReadMode = PipeTransmissionMode.Byte;
            _messageConcurrentQueue = new ConcurrentQueue<string>();
            _autoResetEvent = new AutoResetEvent(false);
            _parentProcessId = parentProcessId;
        }

        public void Start()
        {
            Task.Factory.StartNew(DoSend);
        }

        public void Stop()
        {
            _stop = true;
        }

        public void SendMessage(string message)
        {
            _messageConcurrentQueue.Enqueue(message);
            _autoResetEvent.Set();
        }

        void DoSend()
        {
            using (StreamWriter sw = new StreamWriter(_pipeClient))
            {
                while (!_stop)
                {
                    if (_parentProcessId != -1)
                    {
                        try
                        {
                            Process.GetProcessById(_parentProcessId);
                        }
                        catch
                        {
                            ParentProcessExit?.Invoke();
                        }
                    }
                    _autoResetEvent.Reset();
                    while (_messageConcurrentQueue.TryDequeue(out var msg))
                    {
                        sw.WriteLine(msg);
                        sw.Flush();
                        _pipeClient.WaitForPipeDrain();
                    }
                    _autoResetEvent.WaitOne(1000);
                }
            }
        }
    }
}
