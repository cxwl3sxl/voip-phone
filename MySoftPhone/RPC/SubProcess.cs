﻿using System;
using System.Diagnostics;

namespace MySoftPhone.RPC
{
    //父进程创建的子进程
    public class SubProcess
    {
        private readonly ProcessStartInfo _processStartInfo;
        private readonly string _name;
        private bool _isSubProcessExit;
        private Process _subProcess;
        private readonly string _inputArgs;

        private AnonymousPipeServer _anonymousPipeServer;

        public SubProcess(ProcessStartInfo processStartInfo, string name)
        {
            _inputArgs = processStartInfo.Arguments;
            _processStartInfo = processStartInfo;
            _name = name;
            _processStartInfo.UseShellExecute = false;
        }

        public void Start()
        {
            _anonymousPipeServer?.Stop();
            _anonymousPipeServer = new AnonymousPipeServer();
            _anonymousPipeServer.MessageReceived += _anonymousPipeServer_MessageReceived;
            var serverInfo = _anonymousPipeServer.GetServerInfo();
            var newArgs =
                $"${serverInfo} ${Process.GetCurrentProcess().Id} {_inputArgs}";
            _processStartInfo.Arguments = newArgs;
            _subProcess = new Process
            {
                StartInfo = _processStartInfo,
                EnableRaisingEvents = true
            };
            _subProcess.Exited += SubProcess_Exited;
            _subProcess.Start();
            Trace.Write($"[MyPhone] {_name}的子进程已启动：{_subProcess.Id}");
            _isSubProcessExit = false;
            _anonymousPipeServer.AfterSubProcessStart();
            _anonymousPipeServer.Start();
        }

        private void _anonymousPipeServer_MessageReceived(string obj)
        {
            MessageReceived?.Invoke(this, obj);
        }

        private void SubProcess_Exited(object sender, EventArgs e)
        {
            if (!_isSubProcessExit)
                SubProcessExited?.Invoke(this);
        }

        public void Stop()
        {
            Trace.Write($"[MyPhone] 正在终止{_name}的子进程：{_subProcess.Id}");
            _anonymousPipeServer?.Stop();
            try
            {
                if (!_subProcess.HasExited)
                    _subProcess.Kill();
            }
            catch
            {
            }
        }

        public void SendMessage(string msg)
        {
            _anonymousPipeServer?.SendMessage(msg);
        }

        public int? ProcessId => _subProcess?.Id;

        public event Action<SubProcess> SubProcessExited;
        public event Action<SubProcess, string> MessageReceived;
    }
}
