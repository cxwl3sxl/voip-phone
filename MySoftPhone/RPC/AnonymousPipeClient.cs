using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace MySoftPhone.RPC
{
    public class AnonymousPipeClient
    {
        private readonly List<string> _otherArgs;
        private readonly PipeStream _readPipeStream;
        private readonly PipeStream _writePipeStream;
        public event Action<string> MessageReceived;
        private readonly ConcurrentQueue<string> _messageConcurrentQueue = new ConcurrentQueue<string>();
        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private bool _stop;

        public AnonymousPipeClient(string arg) : this(new[] { arg })
        {
        }

        public AnonymousPipeClient(string[] argStrings)
        {
            if (argStrings == null) throw new ArgumentNullException(nameof(argStrings));
            var pipeServerInfo = "";
            _otherArgs = new List<string>();
            foreach (var argString in argStrings)
            {
                if (argString.StartsWith(AnonymousPipeServer.ArgsPrefx))
                {
                    pipeServerInfo = argString.Replace(AnonymousPipeServer.ArgsPrefx, "");
                    continue;
                }
                _otherArgs.Add(argString);
            }
            if (string.IsNullOrWhiteSpace(pipeServerInfo)) throw new InvalidOperationException("输入参数中没有包含服务器信息的相关参数");
            var info = pipeServerInfo.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (info.Length != 2) throw new InvalidOperationException("服务器参数信息不正确");

            _readPipeStream = new AnonymousPipeClientStream(PipeDirection.In, info[0]);
            if (_readPipeStream == null) throw new Exception("参数无效，无法和主进程通信！");
            _readPipeStream.ReadMode = PipeTransmissionMode.Byte;

            _writePipeStream = new AnonymousPipeClientStream(PipeDirection.Out, info[1]);
            if (_writePipeStream == null) throw new Exception("参数无效，无法和主进程通信！");
            _writePipeStream.ReadMode = PipeTransmissionMode.Byte;
        }

        public string[] OtherArgs => _otherArgs?.ToArray();

        public void Start()
        {
            Task.Factory.StartNew(DoSend);
            Task.Factory.StartNew(DoRead);
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
            using (StreamWriter sw = new StreamWriter(_writePipeStream))
            {
                while (!_stop)
                {
                    try
                    {
                        _autoResetEvent.Reset();
                        while (_messageConcurrentQueue.TryDequeue(out var msg))
                        {
                            sw.WriteLine(msg);
                            sw.Flush();
                            _writePipeStream.WaitForPipeDrain();
                        }
                        _autoResetEvent.WaitOne(1000);
                    }
                    catch (Exception exception)
                    {
                        Trace.TraceError("[[MSP-SERVER-W]]" + exception);
                    }
                }
            }
        }

        void DoRead()
        {
            using (var pipeReader = new StreamReader(_readPipeStream))
            {
                while (!_stop)
                {
                    try
                    {
                        var msg = pipeReader.ReadLine();
                        if (!string.IsNullOrWhiteSpace(msg))
                            MessageReceived?.BeginInvoke(msg, null, null);
                        Thread.Sleep(10);
                    }
                    catch (Exception exception)
                    {
                        Trace.TraceError("[[MSP-SERVER-R]]" + exception);
                    }
                }
            }
        }
    }
}
