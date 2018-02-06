using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using MySoftPhone.RPC;

namespace ConsoleApp1
{
    partial class Program
    {
        private static ParentProcess _parentProcess;

        static void WorkAsClient(string[] args)
        {
            Debugger.Launch();
            _parentProcess = new ParentProcess(args);
            _parentProcess.MessageReceived += _parentProcess_MessageReceived;
            _parentProcess.ParentProcessExit += _parentProcess_ParentProcessExit;
            ClientOutput(string.Join(" ", _parentProcess.InputArgs));
            while (true)
            {
                _parentProcess.SendMessage(DateTime.Now.ToString());
                Thread.Sleep(2000);
            }
        }

        private static void _parentProcess_ParentProcessExit()
        {
            Process.GetCurrentProcess().Kill();
        }

        private static void _parentProcess_MessageReceived(string obj)
        {
            ClientOutput($"收到来自服务器的消息：{obj}");
        }

        static void ClientOutput(string msg)
        {
            Console.WriteLine($"[客户端]:{msg}");
        }
    }
}
