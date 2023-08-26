using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program
    {
        private static readonly char[] ProgressChars = new char[] { '|', '/', '-', '\\' };
        private static int Progress = 0;

        public static UpdateFrequency Next() => UpdateFrequency.Update1;
        public static UpdateFrequency Once() => UpdateFrequency.Once;
        public static UpdateFrequency Update10() => UpdateFrequency.Update10;
        public static UpdateFrequency Update100() => UpdateFrequency.Update100;
        public static bool FloatEquals(float a, float b, float delta = 0.0001f) => Math.Abs(b - a) < delta;

        public static IEnumerable<UpdateFrequency> Enumerate(params Func<IEnumerable<UpdateFrequency>>[] factories)
        {
            foreach (var factory in factories)
            {
                foreach (var update in factory())
                {
                    yield return update;
                }
            }
        }

        public static MyIni ReadConfiguration(string customData)
        {
            MyIni configuration = new MyIni();
            MyIniParseResult config;
            if (!configuration.TryParse(customData, out config))
            {
                throw new Exception(config.ToString());
            }

            return config.IsDefined ? configuration : null;
        }

        private Dictionary<string, Func<IEnumerator<UpdateFrequency>>> Commands { get; set; } =
            new Dictionary<string, Func<IEnumerator<UpdateFrequency>>>();
        private string AutoRunCommand = null;
        private MyIni Config;
        private bool Initialized = false;
        private IEnumerator<UpdateFrequency> Operation;
        private LcdManager Output;
        private string Command;
        private MyCommandLine CommandLine = new MyCommandLine();
        private Action<string> Debug;

        private IEnumerable<UpdateFrequency> Enumerate(IEnumerator<UpdateFrequency> source)
        {
            while (source.MoveNext())
            {
                yield return source.Current;
            }
        }

        private IEnumerator<UpdateFrequency> InitializeProgram()
        {
            if (Initialized) yield break;

            Debug = this.Echo;
            Echo = this.InitDebug();
            Output = new LcdManager(this, Me, 0);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            Config = ReadConfiguration(Me.CustomData);
            yield return Next();

            if (Commands.ContainsKey("initialize"))
            {
                foreach (var update in Enumerate(Commands["initialize"]()))
                {
                    yield return update;
                }
            }

            Initialized = true;
        }

        private void RunCommand(string command)
        {
            Func<IEnumerator<UpdateFrequency>> handlerFactory;
            if (Commands.TryGetValue(command, out handlerFactory))
            {
                Operation?.Dispose();
                Operation = handlerFactory();
            }
            else
            {
                Echo($"Unrecognized command: {command}");
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
            try
            {
                if (Initialized && this.IsCommand(updateSource))
                {
                    if (CommandLine.TryParse(argument))
                    {
                        Command = CommandLine.Argument(0);
                        RunCommand(Command);
                    }
                }
                else if (Initialized && Operation == null && AutoRunCommand != null)
                {
                    RunCommand(AutoRunCommand);
                }
                else if (!Initialized && Operation == null)
                {
                    Operation = InitializeProgram();
                }

                Operation = this.RunOperation(Operation);

                if (Initialized && Operation == null && AutoRunCommand != null)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Once;
                }

                Debug(ProgressChars[Progress].ToString());
                Progress = Progress == ProgressChars.Length - 1 ? 0 : Progress + 1;
            }
            catch (Exception ex)
            {
                Echo(ex.ToString());
                throw;
            }
        }
    }
}
 