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

        public Program() : base()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;
            Commands.Add("initialize", Initialize);
            Commands.Add("close", CloseAirlocks);
            Commands.Add("cycle", CycleAirlocks);
            Commands.Add("on", TurnOn);
            Commands.Add("off", TurnOff);
            Commands.Add("onoff", TurnOnOff);
        }

        private IEnumerator<UpdateFrequency> Initialize()
        {
            Me.SetScriptTitle("Airlocks");
            Output.AddTextSurfaces("Airlock Control");
            Output.WriteTitle("Airlock Control");
            yield return Next();

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

                    yield return Next();
                }
            }

            Output.Write($"Found {Airlocks.Count} airlocks.");
            Initialized = true;
        }

        private IEnumerator<UpdateFrequency> CloseAirlocks()
        {
            yield return Update100();

            Airlocks.ForEach(a => a.Close());
            yield break;
        }

        private IEnumerator<UpdateFrequency> CycleAirlocks()
        {
            Output.WriteTitle("Airlock Control: Cycle Airlocks");
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
            yield return Next();

            Output.Write("Closing airlock doors...");
            foreach (var airlock in Airlocks)
            {
                airlock.Close();
            }
            yield return Next();

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
            else
            {
                Output.Write("All airlock vents are disabled.");
            }
            yield return Next();

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
                    if (airlock.IsInnerDoor(oppositeDoor) && !airlock.IsAtInnerPressure)
                    {
                        Output.Write($"Unable to open {oppositeDoor.CustomName}:");
                        Output.Write("- Airlock is not at inner pressure.");
                    }
                    else
                    {
                        oppositeDoor.OpenDoor();
                        Output.Write($"Open {oppositeDoor.CustomName}...");
                    }
                }
            }

            yield return Next();
            Output.Write("Done.");
        }


        private IEnumerator<UpdateFrequency> TurnOn()
        {
            Output.WriteTitle("Airlock Control: Turn On");
            Airlocks.ForEach(a => a.Enabled = true);
            Output.Write("All airlocks enabled.");
            yield break;
        }


        private IEnumerator<UpdateFrequency> TurnOff()
        {
            Output.WriteTitle("Airlock Control: Turn Off");
            Airlocks.ForEach(a => a.Enabled = false);
            Output.Write("All airlocks disabled.");
            yield break;
        }

        private IEnumerator<UpdateFrequency> TurnOnOff()
        {
            return Airlocks.Any(a => a.Enabled) ? TurnOff() : TurnOn();
        }
    }
}
