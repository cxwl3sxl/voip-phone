using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using MySoftPhone.RPC;

namespace MySoftPhone
{
    class Program
    {
        private static Logger _logger;

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                _logger = new Logger("MyPhone-Main");
                App app = new App();
                app.InitializeComponent();
                app.Run();
            }
            else
            {
                _logger = new Logger($"MyPhone-Sub");
                _logger.Information($"子进程正在启动，参数：{string.Join(" ", args)}");
                if (File.Exists("debug.txt"))
                {
                    MessageBox.Show($"请附加进程：{Process.GetCurrentProcess().Id} 然后点击确定开始调试");
                }

                _parentProcess = new ParentProcess(args);
                _parentProcess.MessageReceived += ParentProcess_MessageReceived;
                _parentProcess.ParentProcessExit += ParentProcess_ParentProcessExit;
                if (_parentProcess.InputArgs.Length != 5)
                {
                    _logger.Warning("参数数量错误，即将关闭");
                    return;
                }

                try
                {
                    _logger = new Logger($"MyPhone-Sub-{_parentProcess.InputArgs[1]}");
                    _realPhone = new RealPhone(_parentProcess.InputArgs[0],
                        _parentProcess.InputArgs[1],
                        _parentProcess.InputArgs[2],
                        _parentProcess.InputArgs[3],
                        Convert.ToInt32(_parentProcess.InputArgs[4]));
                    _realPhone.StateChanged += _realPhone_StateChanged;
                    _realPhone.StateMessageChanged += _realPhone_StateMessageChanged;
                    _realPhone.RaiseMessage += _realPhone_RaiseMessage;
                }
                catch (Exception ex)
                {
                    _logger.Error($"子进程初始化错误", ex);
                    _parentProcess.SendMessage($"$I:{ex.Message}");
                }

                while (!_exit)
                {
                    Thread.Sleep(500);
                }

                _realPhone?.Dispose();

                _logger.Information("子进程已退出");
            }
        }

        private static bool _exit;
        private static void _realPhone_RaiseMessage(string obj)
        {
            _logger.Information($"正在向父进程发送消息：{obj}");
            _parentProcess.SendMessage($"$E:RaiseMessage#{obj}");
        }

        private static void _realPhone_StateMessageChanged(string obj)
        {
            _logger.Information($"正在向父进程发送状态消息：{obj}");
            _parentProcess.SendMessage($"$E:StateMessageChanged#{obj}");
        }

        private static void _realPhone_StateChanged(PhoneState obj)
        {
            _logger.Information($"正在向父进程发送状态消息：{obj}");
            _parentProcess.SendMessage($"$E:StateChanged#{obj}");
        }

        private static RealPhone _realPhone = null;
        private static ParentProcess _parentProcess;

        private static void ParentProcess_ParentProcessExit()
        {
            _logger.Warning("检测到父进程退出");
            _exit = true;
        }

        private static void ParentProcess_MessageReceived(string obj)
        {
            var msgType = obj.Substring(0, 3);
            switch (msgType)
            {
                case "$E:":
                    ProcessEvent(obj.Substring(3));
                    break;
                case "$C:":
                    ProcessCall(obj.Substring(3));
                    break;
                case "$R:":
                    ProcessReturn(obj.Substring(3));
                    break;
            }
        }

        static void ProcessCall(string callInfo)
        {
            var args = callInfo.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            var callId = args[0];
            switch (args[1])
            {
                case "SendSignal":
                    _realPhone?.SendSignal(Convert.ToInt32(args[2]));
                    break;
                case "StopSignal":
                    _realPhone?.StopSignal();
                    break;
                case "PickUp":
                    //Debugger.Break();
                    _realPhone?.PickUp(args.Length == 3 ? args[2] : "");
                    break;
                case "HangUp":
                    _realPhone?.HangUp();
                    break;
                case "HaveCall":
                    _parentProcess.SendMessage($"$R:{callId}#{_realPhone?.HaveCall ?? false}");
                    break;
                case "IsInCall":
                    _parentProcess.SendMessage($"$R:{callId}#{_realPhone?.IsInCall ?? false}");
                    break;
            }
        }

        static void ProcessEvent(string eventInfo)
        {
            throw new NotSupportedException();
        }

        static void ProcessReturn(string returnInfo)
        {
            throw new NotSupportedException();
        }
    }
}
