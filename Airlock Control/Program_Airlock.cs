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
        private readonly List<Airlock> Airlocks = new List<Airlock>();

        public Program()
        {
            Echo = this.InitDebug();
            Output = new LcdManager(this, Me, 0);
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        private IEnumerator<UpdateFrequency> Initialize()
        {
            if (Initialized) yield break;
            foreach (var update in Enumerate(InitializeCommon()))
            {
                yield return update;
            }

            Commands.Add("close", CloseAirlocks);
            Commands.Add("cycle", CycleAirlocks);

            Output.AddTextSurfaces("Airlock Control");
            Output.WriteTitle("Airlock Control");
            this.InitConfiguration(Config);
            yield return UpdateFrequency.Once;


            var allVents = this.GetBlocksOfType<IMyAirVent>(v => v.IsSameConstructAs(Me));
            foreach (var vent in allVents)
            {
                if (vent.CustomName.Contains(" Airlock Vent"))
                {
                    string baseName = vent.CustomName.Substring(0, vent.CustomName.IndexOf(" Airlock Vent"));
                    var airlockConfig = new MyIni();
                    GridExtensions.InitConfiguration(vent.CustomData, airlockConfig);
                    if (!airlockConfig.Get("Airlock", "Ignore").ToBoolean(false))
                    {
                        Output.Write($"Adding {baseName}...");
                        Airlocks.Add(new Airlock(this, baseName, airlockConfig));
                    }
                    else
                    {
                        Output.Write($"Ignore {baseName}");
                    }

                    yield return UpdateFrequency.Once;
                }
            }

            Output.Write($"Found {Airlocks.Count} airlocks.");
            Initialized = true;
        }

        private IEnumerator<UpdateFrequency> CloseAirlocks()
        {
            Airlocks.ForEach(a => a.Close());
            yield break;
        }

        private IEnumerator<UpdateFrequency> CycleAirlocks()
        {
            Output.WriteTitle("Cycle Airlocks");
            var entryDoors = Airlocks.Select(a => a.EntryDoor).Where(d => d != null).ToList();
            bool? entering = null;
            if (entryDoors.Count > 0)
            {
                entering = entryDoors.Any(d => d.CustomName.Contains("Outer"));
                Output.Write($"{(entering != false ? "Entering" : "Exiting")} through:");
                foreach (var door in entryDoors)
                {
                    Output.Write($"- {door.CustomName}");
                }
            }
            else
            {
                Output.Write("Entry door not detected.");
            }
            yield return UpdateFrequency.Once;

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
            if (Airlocks.Any(a => a.Enabled))
            {
                Output.Write(entering == false || Airlocks.FirstOrDefault(a => a.Enabled)?.IsPressurizing == true ?
                    "Depressurizing..." :
                    "Pressurizing...");
            }
            yield return UpdateFrequency.Once;

            var pressurize = entering == true || Airlocks.First().Depressurize;
            foreach (var airlock in Airlocks)
            {
                airlock.TogglePressure(pressurize);
            }

            yield return UpdateFrequency.Update100;
            while (Airlocks.Any(a => a.Enabled && !a.IsAtPressure))
            {
                yield return UpdateFrequency.Update10;
            }

            foreach (var airlock in Airlocks)
            {
                if (entryDoors.Any(d => airlock.Doors.Contains(d)))
                {
                    var oppositeDoor = airlock.Doors.First(d => !entryDoors.Contains(d));
                    oppositeDoor.OpenDoor();
                    Output.Write($"Open {oppositeDoor.CustomName}...");
                }
            }

            yield return UpdateFrequency.Once;
            Output.Write("Done.");
        }
    }
}
