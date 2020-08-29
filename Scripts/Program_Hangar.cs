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

        private readonly MyIni Config = new MyIni();
        private bool Initialized = false;
        private IList<IMyDoor> HangarDoors;
        private IList<IMyAirVent> HangarAirVents;
        private IList<IMyInteriorLight> HangarLights;
        private IMyAirVent Vent;
        private readonly LcdManager Output;
        private IEnumerator<UpdateFrequency> InitOperation;
        private IEnumerator<UpdateFrequency> HangarDoorOperation;

        public Program()
        {
            Echo = this.InitDebug();
            Output = new LcdManager(this, Me, 0);
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        private IEnumerator<UpdateFrequency> Init()
        {
            if (Initialized) yield break;

            Output.WriteTitle("Hangar Control");
            this.InitConfiguration(Config);
            yield return UpdateFrequency.Once;

            string prefix = Config.Get(ConfigSectionName, "prefix").ToString();
            HangarDoors = this.GetBlocks<IMyDoor>($"{prefix} Hangar Door");
            Output.Write($"Found {HangarDoors.Count} hangar doors.");
            yield return UpdateFrequency.Once;

            HangarAirVents = this.GetBlocks<IMyAirVent>($"{prefix} Hangar Air Vent");
            Vent = HangarAirVents[0];
            Output.Write($"Found {HangarAirVents.Count} vents.");
            yield return UpdateFrequency.Once;
            
            HangarLights = this.GetBlocks<IMyInteriorLight>($"{prefix} Hangar Ceiling Light");
            Output.Write($"Found {HangarLights.Count} lights.");
            yield return UpdateFrequency.Once;

            Initialized = true;
        }

        private void ToggleHangarDoors()
        {
            if (HangarDoors.Any(hd => hd.Status == DoorStatus.Open || hd.Status == DoorStatus.Opening))
            {
                HangarDoorOperation?.Dispose();
                HangarDoorOperation = CloseHangarDoors();
            }
            else
            {
                HangarDoorOperation?.Dispose();
                HangarDoorOperation = OpenHangarDoors();
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
            try
            {
                if (Initialized && this.IsCommand(updateSource))
                {
                    switch (argument) 
                    {
                        case "openclose":
                            ToggleHangarDoors();
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

                HangarDoorOperation = this.RunOperation(HangarDoorOperation);
                InitOperation = this.RunOperation(InitOperation);
            }
            catch (Exception ex)
            {
                Echo(ex.ToString());
                throw;
            }
        }

        private IEnumerator<UpdateFrequency> CloseHangarDoors()
        {
            Output.WriteTitle("Close Hangar Doors");

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
                yield return UpdateFrequency.Update10;
            }

            Output.Write("Doors closed.");
            yield return UpdateFrequency.Once;

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
                        yield return UpdateFrequency.Update10;
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

                yield return UpdateFrequency.Once;

                foreach (var v in HangarAirVents)
                {
                    v.Depressurize = true;
                }

                while (Vent.GetOxygenLevel() > 0)
                {
                    yield return UpdateFrequency.Update10;
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
                yield return UpdateFrequency.Update10;
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
