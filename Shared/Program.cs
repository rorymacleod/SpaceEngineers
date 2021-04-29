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
        private Dictionary<string, Func<IEnumerator<UpdateFrequency>>> Commands { get; set; } =
            new Dictionary<string, Func<IEnumerator<UpdateFrequency>>>();
        private string AutoRunCommand = null;
        private readonly MyIni Config = new MyIni();
        private bool Initialized = false;
        private IEnumerator<UpdateFrequency> Operation;
        private readonly LcdManager Output;

        public Program() : base()
        {
            Echo = this.InitDebug();
            Output = new LcdManager(this, Me, 0);
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        private IEnumerable<UpdateFrequency> Enumerate(IEnumerator<UpdateFrequency> source)
        {
            while (source.MoveNext())
            {
                yield return source.Current;
            }
        }

        private IEnumerator<UpdateFrequency> InitializeCommon()
        {
            if (Initialized) yield break;

            ReadConfiguration(Me.CustomData, Config);
            yield return UpdateFrequency.Once;
        }

        private void ReadConfiguration(string customData, MyIni configuration)
        {
            MyIniParseResult config;
            if (!configuration.TryParse(customData, out config))
            {
                throw new Exception(config.ToString());
            }
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
                    RunCommand(argument.ToLower());
                }
                else if (Initialized && Operation == null && AutoRunCommand != null)
                {
                    RunCommand(AutoRunCommand);
                }
                else if (!Initialized && Operation == null)
                {
                    Operation = Initialize();
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
 