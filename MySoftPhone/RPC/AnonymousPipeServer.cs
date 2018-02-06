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
    public class AnonymousPipeServer
    {
        public const string ArgsPrefx = "$$$$####8899";

        private readonly AnonymousPipeServerStream _writePipeServerStream;
        private readonly AnonymousPipeServerStream _readPipeServerStream;

        public event Action<string> MessageReceived;
        private readonly ConcurrentQueue<string> _messageConcurrentQueue = new ConcurrentQueue<string>();
        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);


        private bool _stop;

        public AnonymousPipeServer()
        {
            _writePipeServerStream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable)
            {
                ReadMode = PipeTransmissionMode.Byte
            };
            _readPipeServerStream = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable)
            {
                ReadMode = PipeTransmissionMode.Byte
            };
        }

        public string GetServerInfo()
        {
            return
                $"{ArgsPrefx}{_writePipeServerStream.GetClientHandleAsString()}-{_readPipeServerStream.GetClientHandleAsString()}";
        }

        public void AfterSubProcessStart()
        {
            _writePipeServerStream.DisposeLocalCopyOfClientHandle();
            _readPipeServerStream.DisposeLocalCopyOfClientHandle();
        }

        public void SendMessage(string message)
        {
            _messageConcurrentQueue.Enqueue(message);
            _autoResetEvent.Set();
        }

        public void Start()
        {
            Task.Factory.StartNew(DoSend);
            Task.Factory.StartNew(DoRead);
        }

        public void Stop()
        {
            _stop = true;
        }

        void DoSend()
        {
            using (StreamWriter sw = new StreamWriter(_writePipeServerStream))
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
                            _writePipeServerStream.WaitForPipeDrain();
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
            using (var pipeReader = new StreamReader(_readPipeServerStream))
            {
                while (!_stop)
                {
                    try
                    {
                        var msg = pipeReader.ReadLine();
                        if (!string.IsNullOrWhiteSpace(msg))
                            MessageReceived?.BeginInvoke(msg, null, null);
                        Thread.Sleep(500);
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
