using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace MySoftPhone.RPC
{
    public class SubProcess
    {
        private readonly ProcessStartInfo _processStartInfo;
        private AnonymousPipeServerStream _anonymousPipeServerStream;
        private StreamReader _pipeReader;
        private bool _isSubProcessExit;
        private Process _subProcess;
        private readonly string _inputArgs;

        public SubProcess(ProcessStartInfo processStartInfo)
        {
            _inputArgs = processStartInfo.Arguments;
            _processStartInfo = processStartInfo;
            _processStartInfo.UseShellExecute = false;
        }

        public void Start()
        {
            _anonymousPipeServerStream?.Dispose();
            _anonymousPipeServerStream =
                new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable)
                {
                    ReadMode = PipeTransmissionMode.Byte
                };
            _pipeReader?.Dispose();
            _pipeReader = new StreamReader(_anonymousPipeServerStream);
            var newArgs =
                $"${_anonymousPipeServerStream.GetClientHandleAsString()} ${Process.GetCurrentProcess().Id} {_inputArgs}";
            _processStartInfo.Arguments = newArgs;
            _subProcess = new Process
            {
                StartInfo = _processStartInfo,
                EnableRaisingEvents = true
            };
            _subProcess.Exited += SubProcess_Exited;
            _subProcess.Start();
            _isSubProcessExit = false;
            _anonymousPipeServerStream.DisposeLocalCopyOfClientHandle();
            Task.Factory.StartNew(ReadMessage);
        }

        private void SubProcess_Exited(object sender, EventArgs e)
        {
            if (!_isSubProcessExit)
                SubProcessExited?.Invoke(this);
        }

        void ReadMessage()
        {
            while (!_isSubProcessExit)
            {
                var msg = _pipeReader?.ReadLine();
                if (!string.IsNullOrWhiteSpace(msg))
                    MessageReceived?.Invoke(this, msg);
                Thread.Sleep(500);
            }
        }

        public void Stop()
        {
            _isSubProcessExit = true;
            _anonymousPipeServerStream?.Dispose();
            _pipeReader?.Dispose();
            _subProcess.Kill();
        }

        public int? ProcessId => _subProcess?.Id;

        public event Action<SubProcess> SubProcessExited;
        public event Action<SubProcess, string> MessageReceived;
    }
}
