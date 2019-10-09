using System;
using ScriptEngine.HostedScript.Library;
using ScriptEngine.HostedScript;
using ScriptEngine;
using System.IO;
using System.Windows.Forms;

namespace HotkeyWin
{
    class ScriptCore : IHostApplication
    {
        private HostedScriptEngine engine;
        private static readonly ScriptCore instance = new ScriptCore();
        public string Name { get; private set; }
        internal ScriptingEngine EngineInstance { get; set; }

        private string[] scriptParams = new string[0];

        private ScriptCore()
        {
        }

        public static ScriptCore GetInstance()
        {
            return instance;
        }

        public bool RunAction(string actionInfo)
        {

            Name = System.Guid.NewGuid().ToString();
            engine = new HostedScriptEngine();
            engine.Initialize();

            var script = engine.Loader.FromFile(@"core/ScriptRunner.os");
            var process = engine.CreateProcess(ScriptCore.GetInstance(), script);
            var ev = new EnvironmentVariablesImpl();
            ev.SetEnvironmentVariable("StartParams", actionInfo);
            process.Start();

            return true;
        }

        public void Echo(string str, MessageStatusEnum status = MessageStatusEnum.Ordinary)
        {
            DateTime curDate = DateTime.Now;
            Console.WriteLine(curDate + ": " + str);

            StreamWriter myfile = new StreamWriter("SmartConfigurator2.log", true);
            myfile.WriteLine(curDate + ": " + str);
            myfile.Close();
        }

        public string[] GetCommandLineArguments()
        {
            return scriptParams;
        }

        public bool InputString(out string result, int maxLen)
        {
            throw new NotImplementedException();
        }

        public void ShowExceptionInfo(Exception exc)
        {
            Echo(exc.ToString());
        }
    }
}
