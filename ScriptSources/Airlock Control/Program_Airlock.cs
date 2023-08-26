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
using System.Security.Policy;

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
            if (!Airlocks.Any(a => a.Enabled))
            {
                Output.Write("All airlocks are disabled.");
                yield break;
            }

            var entryDoor = FindEntryDoor();
            Output.Write(entryDoor == null
                ? "Entry door not detected."
                : $"Entered {entryDoor.CustomName}");

            var activeAirlock = GetAirlockByDoor(entryDoor);
            var exitDoor = GetExitDoor(activeAirlock, entryDoor);
            bool entering = IsEntering(entryDoor);
            var exitZone = GetExitZone(activeAirlock, entering);
            yield return Next();

            Output.Write("Closing airlock doors...");
            CloseAirlocks(Airlocks);
            yield return Next();

            while (exitZone.Airlocks.Any(a => !a.IsClosed))
            {
                yield return Update10();
            }
            Output.Write("Airlock doors closed.");

            yield return Update10();
            Output.Write($"{(exitZone.IsPressurized ? "Pressurizing" : "Depressurizing")} " +
                $"{exitZone.Airlocks.Count} airlocks ");
            Output.Write($"  to {exitZone.Name} pressure...");
            SetAirlocksToZonePressure(exitZone);
            int i = 0;
            while (!(activeAirlock ?? exitZone.Airlocks.First()).IsAtPressure || i++ > 50)
            {
                yield return Update10();
            }
            yield return Update100();

            OpenDoor(exitDoor);

            Output.Write("Done.");
        }

        private IMyDoor FindEntryDoor()
        {
            var entryDoor = Airlocks.Select(a => a.EntryDoor).Where(d => d != null).FirstOrDefault();
            return entryDoor;
        }

        private bool IsEntering(IMyDoor entryDoor)
        {
            if (entryDoor == null)
            {
                bool engineerIsInside = Airlocks.Any(a => a.IsPressurizing);
                return !engineerIsInside;
            }

            return entryDoor.CustomName.Contains("Outer");
        }

        private IMyDoor GetExitDoor(Airlock airlock, IMyDoor entryDoor)
        {
            if (airlock == null)
            {
                return null;
            }

            var exitDoor = airlock.Doors.FirstOrDefault(d => d != entryDoor);
            return exitDoor;
        }

        private AirZone GetExitZone(Airlock airlock, bool entering)
        {
            AirZone zone;
            if (airlock != null)
            {
                var zoneVent = entering ? airlock.InnerVent : airlock.OuterVent;
                var group = Airlocks.Where(a => entering ? a.InnerVent == zoneVent : a.OuterVent == zoneVent);
                zone = new AirZone
                {
                    Airlocks = group.ToList(),
                    Vent = zoneVent,
                    Name = GetZoneName(zoneVent, entering),
                };
            }
            else
            {
                zone = new AirZone
                {
                    Airlocks = Airlocks,
                    DefaultPressure = entering,
                    Name = GetZoneName(null, entering),
                };
            }

            return zone;
        }


        private string GetZoneName(IMyAirVent vent, bool entering)
        {
            if (vent == null)
            {
                return entering ? "interior" : "exterior";
            }

            string name = vent.CustomName;
            if (name.Length > 3 && name[2] == ' ')
            {
                name = name.Substring(3);
            }

            int ix = name.IndexOf(" Air Vent");
            ix = ix > -1 ? ix : name.IndexOf(" Vent");
            if (ix > -1)
            {
                name = name.Substring(0, ix);
            }

            return name;
        }

        private Airlock GetAirlockByDoor(IMyDoor door)
        {
            if (door == null)
            {
                return null;
            }

            var airlock = Airlocks.FirstOrDefault(a => a.Doors.Contains(door));
            return airlock;
        }

        private Airlock GetAirlockByName(string name)
        {
            var airlock = Airlocks.FirstOrDefault(a =>
                string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
            return airlock;
        }

        private void CloseAirlocks(IEnumerable<Airlock> airlocks)
        {
            foreach (var airlock in airlocks)
            {
                airlock.Close();
            }
        }

        private void SetAirlocksToZonePressure(AirZone zone)
        {
            bool pressurize = zone.IsPressurized;
            foreach (var airlock in zone.Airlocks)
            {
                airlock.TogglePressure(pressurize);
            }
        }

        private void OpenDoor(IMyDoor door)
        {
            if (door != null)
            {
                door.OpenDoor();
            }
        }

        private IEnumerator<UpdateFrequency> TurnOn()
        {
            Output.WriteTitle("Airlock Control: Turn On");
            SetEnabled(true);
            yield break;
        }


        private IEnumerator<UpdateFrequency> TurnOff()
        {
            Output.WriteTitle("Airlock Control: Turn Off");
            SetEnabled(false);
            yield break;
        }

        private IEnumerator<UpdateFrequency> TurnOnOff()
        {
            SetEnabled(null);
            yield break;
        }

        private void SetEnabled(bool? state)
        {
            if (CommandLine.ArgumentCount > 1)
            {
                var airlock = GetAirlockByName(CommandLine.Argument(1));
                if (airlock == null)
                {
                    Output.Write($"Airlock \"{CommandLine.Argument(1)}\" not found.");
                }
                else
                {
                    SetEnabled(airlock, state ?? !airlock.Enabled);
                }
            }
            else
            {
                state = state ?? Airlocks.Any(a => !a.Enabled);
                foreach (var airlock in Airlocks)
                {
                    SetEnabled(airlock, state.GetValueOrDefault());
                }
            }
        }

        private void SetEnabled(Airlock airlock, bool state)
        {
            airlock.Enabled = state;
            if (state)
            {
                airlock.Enabled = true;
                Output.Write($"{airlock.Name} airlock enabled.");
            }
            else
            {
                airlock.Enabled = false;
                Output.Write($"{airlock.Name} airlock disabled.");
            }
        }
    }
}
