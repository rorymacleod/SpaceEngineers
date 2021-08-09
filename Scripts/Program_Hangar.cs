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
using System.CodeDom;
using System.IO.Ports;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string ConfigSectionName = "Hangar Control";
        readonly Color LightNormalColor = new Color(255, 255, 220);
        readonly Color LightDangerColor = new Color(151, 0, 0);

        private IList<IMyDoor> HangarDoors;
        private IList<IMyAirVent> HangarAirVents;
        private IList<IMyInteriorLight> HangarLights;
        private IMyAirVent Vent;

        public Program() : base()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;
            Commands.Add("initialize", Initialize);
            Commands.Add("cycle", ToggleHangarDoors);
            Commands.Add("open", OpenHangarDoors);
            Commands.Add("close", CloseHangarDoors);
        }

        private IEnumerator<UpdateFrequency> Initialize()
        {
            Me.SetScriptTitle("Hangar");
            Output.AddTextSurfaces("Hangar Control");
            Output.WriteTitle("Hangar Control");
            yield return Next();

            HangarDoors = this.GetGroupBlocks<IMyDoor>(
                Config.Get(ConfigSectionName, "Hangar door group").ToString());
            Output.Write($"Found {HangarDoors.Count} hangar doors.");
            yield return Next();

            HangarAirVents = this.GetGroupBlocks<IMyAirVent>(
                Config.Get(ConfigSectionName, "Air vent group").ToString());
            Vent = HangarAirVents[0];
            Output.Write($"Found {HangarAirVents.Count} vents.");
            yield return Next();
            
            HangarLights = this.GetGroupBlocks<IMyInteriorLight>(
                Config.Get(ConfigSectionName, "Interior light group").ToString());
            Output.Write($"Found {HangarLights.Count} lights.");
            yield return Next();

            Initialized = true;
        }

        private IEnumerator<UpdateFrequency> ToggleHangarDoors()
        {
            if (HangarDoors.Any(hd => hd.Status == DoorStatus.Open || hd.Status == DoorStatus.Opening))
            {
                return CloseHangarDoors();
            }

            return OpenHangarDoors();
        }

        private IEnumerator<UpdateFrequency> CloseHangarDoors()
        {
            Output.WriteTitle("Hangar Control: Close");

            if (HangarDoors.Any(hd => hd.Status != DoorStatus.Closed && hd.Status != DoorStatus.Closing))
            {
                Output.Write("Closing doors...");
                foreach (var hd in HangarDoors)
                {
                    hd.CloseDoor();
                }
            }

            while (HangarDoors.Any(hd => hd.Status != DoorStatus.Closed))
            {
                yield return Update10();
            }

            Output.Write("Doors closed.");
            yield return Next();

            if (Vent.IsWorking && Vent.CanPressurize)
            {
                if (Vent.Status != VentStatus.Pressurized)
                {
                    Output.Write("Pressurizing hangar...");
                    foreach (var v in HangarAirVents)
                    {
                        v.Depressurize = false;
                    }

                    while (Vent.Status != VentStatus.Pressurized)
                    {
                        yield return Update10();
                    }
                }

                Output.Write("Hangar pressurized.");
            }
            else
            {
                Output.Write("Hangar vents are disabled.");
            }

            SetLightColor(HangarLights, LightNormalColor);
            Output.Write("Hangar doors closed.");
        }

        private IEnumerator<UpdateFrequency> OpenHangarDoors()
        {
            Output.WriteTitle("Open Hangar Doors");
            if (!Vent.IsWorking)
            {
                Output.Write("ERROR: Hangar vents are disabled.");
                yield break;
            }

            if (!Vent.CanPressurize)
            {
                Output.Write("ERROR: Hangar is not airtight.");
                yield break;
            }

            if (Vent.GetOxygenLevel() > 0)
            {
                Output.Write("Depressurizing hangar...");
                SetLightColor(HangarLights, LightDangerColor);

                yield return Next();

                foreach (var v in HangarAirVents)
                {
                    v.Depressurize = true;
                }

                while (Vent.GetOxygenLevel() > 0)
                {
                    yield return Update10();
                }

                Output.Write("Hangar depressurized.");
            }

            Output.Write("Opening doors...");
            foreach (var hd in HangarDoors)
            {
                hd.OpenDoor();
            }

            while (HangarDoors.Any(hd => hd.Status != DoorStatus.Open))
            {
                yield return Update10();
            }

            Output.Write("Hangar doors open.");
        }

        private void SetLightColor(IList<IMyInteriorLight> lights, Color color)
        {
            foreach (var l in lights)
            {
                l.Color = color;
            }
        }
    }
}
