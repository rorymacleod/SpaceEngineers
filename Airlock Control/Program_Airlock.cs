using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private const string ConfigSectionName = "Airlock Control";
        private readonly List<Airlock> Airlocks = new List<Airlock>();
        private IEnumerator<UpdateFrequency> AirlockOperation;
        private IEnumerator<UpdateFrequency> InitOperation;
        private readonly MyIni Config = new MyIni();
        private readonly LcdManager Output;
        private bool Initialized = false;

        public Program()
        {
            Echo = this.InitDebug();
            Output = new LcdManager(this, Me, 0);
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        private IEnumerator<UpdateFrequency> Init()
        {
            if (Initialized) yield break;

            Output.WriteTitle("Airlock Control");
            this.InitConfiguration(Config);
            yield return UpdateFrequency.Once;

            int i = 1;
            while (Config.ContainsKey(ConfigSectionName, $"airlock{i}"))
            {
                string prefix = Config.Get(ConfigSectionName, $"airlock{i}").ToString();
                string name = Config.Get(ConfigSectionName, $"airlock{i}Name").ToString(prefix);
                Airlocks.Add(new Airlock(this, prefix)
                {
                    Name = name,
                });
                i++;
                yield return UpdateFrequency.Once;
            }

            Output.Write($"Found {Airlocks.Count} airlocks:");
            foreach (var airlock in Airlocks)
            {
                Output.Write(airlock.Name);
            }
            Initialized = true;
        }

        private IEnumerator<UpdateFrequency> CycleAirlocks()
        {
            Output.WriteTitle("Cycle Airlocks");
            Output.Write("Closing airlock doors...");
            foreach (var airlock in Airlocks)
            {
                airlock.Close();
            }
            yield return UpdateFrequency.Once;

            while (Airlocks.Any(a => !a.IsClosed))
            {
                yield return UpdateFrequency.Update10;
            }

            Output.Write("Airlock doors closed.");
            Output.Write(Airlocks.FirstOrDefault(a => a.Enabled)?.IsPressurizing == true ?
                "Depressurizing..." :
                "Pressurizing");
            yield return UpdateFrequency.Once;

            foreach (var airlock in Airlocks)
            {
                airlock.TogglePressure();
            }

            yield return UpdateFrequency.Once;
            if (Airlocks.Any(a => !a.IsAtPressure))
            {
                yield return UpdateFrequency.Update10;
            }

            Output.Write("Done.");
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
            try
            {
                if (Initialized && this.IsCommand(updateSource))
                {
                    switch (argument.ToLower())
                    {
                        case "cycle":
                            AirlockOperation?.Dispose();
                            AirlockOperation = CycleAirlocks();
                            break;

                        default:
                            Echo($"Unrecognized command: {argument}");
                            break;
                    }
                }
                else if (!Initialized && InitOperation == null)
                {
                    InitOperation = Init();
                }

                InitOperation = this.RunOperation(InitOperation);
                AirlockOperation = this.RunOperation(AirlockOperation);
            }
            catch (Exception ex)
            {
                Echo(ex.ToString());
                throw;
            }
        }
    }
}
