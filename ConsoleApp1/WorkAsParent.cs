using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using MySoftPhone.RPC;

namespace ConsoleApp1
{
    partial class Program
    {
        private static SubProcess _subProcess;

        static void WorkAsParent()
        {
            _subProcess?.Stop();
            _subProcess = new SubProcess(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location, "hi"));
            _subProcess.MessageReceived += _subProcess_MessageReceived;
            _subProcess.SubProcessExited += _subProcess_SubProcessExited;
            _subProcess.Start();
            ServerOutput("子进程已经启动...");
            while (true)
            {
                _subProcess.SendMessage(DateTime.Now.ToString());
                Thread.Sleep(5000);
            }
        }

        private static void _subProcess_SubProcessExited(SubProcess obj)
        {
            ServerOutput("子进程异常终止，正在重启...");
            WorkAsParent();
        }

        private static void _subProcess_MessageReceived(SubProcess arg1, string arg2)
        {
            ServerOutput($"收到来自子进程（{arg1.ProcessId}）的消息：{arg2}");
        }

        static void ServerOutput(string msg)
        {
            Console.WriteLine($"[服务器]:{msg}");
        }
    }
}
