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
        private List<string> Arguments = new List<string>();

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

        private void ParseArguments(string input)
        {
            var tokens = new List<string>();
            int i = 0, iStart = 0;
            bool inToken = false, inString = false;
            while (i < input.Length)
            {
                if (inToken)
                {
                    if (char.IsWhiteSpace(input[i]))
                    {
                        inToken = false;
                        Echo($"Token: {iStart}, {i - iStart}");
                        tokens.Add(input.Substring(iStart, i - iStart));
                    }
                } 
                else if (inString)
                {
                    if (input[i] == '"' && i > 0 && input[i] != '\\')
                    {
                        inString = false;
                        Echo($"Token: {iStart + 1}, {i - iStart - 1}");
                        tokens.Add(input.Substring(iStart + 1, i - iStart - 1).Replace("\\\"", "\""));
                    }
                }
                else if (!char.IsWhiteSpace(input[i]))
                {
                    inString = input[i] == '"';
                    inToken = !inString;
                    iStart = i;
                }

                i++;
            }

            iStart = input[iStart] == '"' ? iStart + 1 : iStart;
            if (i > iStart && iStart <= input.Length)
            {
                Echo($"Token: {iStart}");
                tokens.Add(input.Substring(iStart));
            }

            if (tokens.Count > 0)
            {
                Command = tokens[0].ToLower();
                Arguments = tokens.Skip(1).ToList();
            }

            Echo("Command: " + Command);
            Arguments.ForEach(a => Echo("Arg: " + a));
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
                    ParseArguments(argument);
                    RunCommand(Command);
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
            }
            catch (Exception ex)
            {
                Echo(ex.ToString());
                throw;
            }
        }
    }
}
 