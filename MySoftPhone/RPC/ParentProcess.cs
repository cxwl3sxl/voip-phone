using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MySoftPhone.RPC
{
    public class ParentProcess : IDisposable
    {
        private readonly int _parentProcessId;
        private bool _stop;
        private readonly ConcurrentQueue<string> _messageConcurrentQueue = new ConcurrentQueue<string>();
        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        private readonly PipeStream _pipeClient;

        public event Action ParentProcessExit;

        public ParentProcess(string[] args)
        {
            Console.WriteLine(string.Join(" ", args));
            if (args.Length > 1 && args[0].StartsWith("$") && args[1].StartsWith("$"))
            {
                _pipeClient = new AnonymousPipeClientStream(PipeDirection.Out, args[0].Substring(1));
                if (_pipeClient == null) throw new Exception("参数无效，无法和主进程通信！");
                _pipeClient.ReadMode = PipeTransmissionMode.Byte;
                _parentProcessId = Convert.ToInt32(args[1].Substring(1));
                InputArgs = args.Skip(2).ToArray();
                Task.Factory.StartNew(DoSend);
            }
            else
            {
                InputArgs = args;
            }
        }

        public string[] InputArgs { get; }

        public void Dispose()
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
                    try
                    {
                        Process.GetProcessById(_parentProcessId);
                    }
                    catch
                    {
                        ParentProcessExit?.Invoke();
                    }
                    _autoResetEvent.Reset();
                    while (_messageConcurrentQueue.TryDequeue(out var msg))
                    {
                        Console.WriteLine("正在写入消息" + msg);
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
