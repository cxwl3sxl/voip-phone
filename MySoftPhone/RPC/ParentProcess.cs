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
    /// <summary>
    /// 子进程拥有的父进程
    /// </summary>
    public class ParentProcess : IDisposable
    {
        private readonly int _parentProcessId;
        private readonly AnonymousPipeClient _anonymousPipeClient;
        public event Action ParentProcessExit;
        public event Action<string> MessageReceived;
        private bool _stop;

        public ParentProcess(string[] args)
        {
            Trace.Write($"[MyPhone] 启动参数：{string.Join(" ", args)}");
            if (args.Length > 1 && args[0].StartsWith("$") && args[1].StartsWith("$"))
            {
                _anonymousPipeClient = new AnonymousPipeClient(args[0].Substring(1));
                _parentProcessId = Convert.ToInt32(args[1].Substring(1));
                InputArgs = args.Skip(2).ToArray();
            }
            else
            {
                InputArgs = args;
            }
            if (_anonymousPipeClient == null) throw new Exception("无法获取服务器连接信息");
            _anonymousPipeClient.MessageReceived += _anonymousPipeClient_MessageReceived;
            _anonymousPipeClient.Start();
            Task.Factory.StartNew(DoCheckParentProcess);
        }

        private void _anonymousPipeClient_MessageReceived(string obj)
        {
            MessageReceived?.Invoke(obj);
        }

        public string[] InputArgs { get; }

        public void Dispose()
        {
            _stop = true;
            _anonymousPipeClient?.Stop();
        }

        public void SendMessage(string message)
        {
            _anonymousPipeClient?.SendMessage(message);
        }

        void DoCheckParentProcess()
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
                Thread.Sleep(500);
            }
        }
    }
}
