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
using VRage.Audio;

namespace IngameScript
{
    partial class Program
    {
        public class Airlock
        {
            private readonly MyGridProgram Grid;
            public readonly IMyDoor InnerDoor;
            public readonly IMyDoor OuterDoor;
            public readonly IMyAirVent Vent;
            public readonly IList<IMyButtonPanel> Buttons;  

            public Airlock(MyGridProgram grid, string prefix)
            {
                Grid = grid;
                InnerDoor = Grid.GetBlock<IMyDoor>(prefix + " Inner Door");
                OuterDoor = Grid.GetBlock<IMyDoor>(prefix + " Outer Door");
                Vent = Grid.GetBlock<IMyAirVent>(prefix + " Vent");
                Buttons = Grid.GetBlocks<IMyButtonPanel>(prefix);
            }

            public bool Enabled => Vent.Enabled;
            public string Name { get; set; }
            public bool IsClosed => !Enabled || (
                InnerDoor.Status == DoorStatus.Closed && 
                OuterDoor.Status == DoorStatus.Closed);
            public bool IsAtPressure => !Enabled || (
                Vent.Status == VentStatus.Depressurized || Vent.Status == VentStatus.Pressurized);
            public bool IsPressurizing => Vent.Enabled && (
                Vent.Status == VentStatus.Pressurizing || Vent.Status == VentStatus.Pressurizing);

            public void Close()
            {
                if (Enabled)
                {
                    InnerDoor.CloseDoor();
                    OuterDoor.CloseDoor();
                }
            }

            public void TogglePressure()
            {
                if (Enabled)
                {
                    Vent.Depressurize = !Vent.Depressurize;
                }
            }

            public void Activate()
            {
                InnerDoor.Enabled = true;
                OuterDoor.Enabled = true;
                Vent.Enabled = true;
                foreach (var b in Buttons)
                {
                    b.ApplyAction("OnOff_Off");
                }
            }
        }
    }
}
