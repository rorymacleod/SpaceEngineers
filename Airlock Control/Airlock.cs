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
            private static readonly Color PressurizedColor = new Color(255, 255, 220);
            private static readonly Color DepressurizedColor = new Color(220, 0, 0);
            private const string ConfigSectionName = "Airlock";

            private readonly MyGridProgram Grid;
            private readonly MyIni Config;
            private readonly string LcdTag;
            public readonly List<IMyDoor> Doors;
            public readonly IMyAirVent Vent;
            public readonly IMyAirVent InnerVent;
            public readonly List<IMyButtonPanel> Buttons;
            public readonly List<IMyLightingBlock> Lights;

            public Airlock(MyGridProgram grid, string prefix, MyIni config)
            {
                Grid = grid;
                Config = config;
                Doors = Grid.GetBlocks<IMyDoor>(prefix + " Airlock Inner Door");
                Doors.AddRange(Grid.GetBlocks<IMyDoor>(prefix + " Airlock Outer Door"));
                Vent = Grid.GetBlock<IMyAirVent>(prefix + " Airlock Vent");
                Buttons = Grid.FindBlocks<IMyButtonPanel>(prefix);
                Lights = new List<IMyLightingBlock>();
                Grid.GridTerminalSystem.GetBlocksOfType(Lights, b => b.CustomName.StartsWith(prefix + " Airlock Light"));
                Name = prefix.IndexOf(" ") == 2 ? prefix.Substring(3) : prefix;
                LcdTag = Config.Get(ConfigSectionName, "LCD tag").ToString("[LCD]");

                Configure();
            }

            public bool Enabled {
                get
                {
                    return Vent.Enabled;
                }
                set
                {
                    Vent.Enabled = value;
                    Doors.ForEach(d => d.Enabled = value);
                    Buttons.ForEach(d => {
                        if (d is IMyFunctionalBlock) {
                            ((IMyFunctionalBlock)d).Enabled = value;
                        }
                    });
                    Lights.ForEach(d => d.Enabled = value);
                }
            }
            public string Name { get; set; }
            public bool IsClosed => !Enabled || (Doors.All(d => d.Status == DoorStatus.Closed));
            public bool IsAtPressure => !Enabled || (
                Vent.Status == VentStatus.Depressurized || Vent.Status == VentStatus.Pressurized ||
                (Vent.Depressurize && Vent.GetOxygenLevel() < 0.01f));
            public bool IsAtInnerPressure => IsAtPressure &&
                (InnerVent == null || InnerVent.Status == Vent.Status);
            public bool IsPressurizing => Vent.Enabled && Vent.Status == VentStatus.Pressurizing;
            public bool Depressurize => Vent.Enabled && Vent.Depressurize;
            public IMyDoor EntryDoor => Doors.Count == 2
                ? Doors.FirstOrDefault(d => d.Status == DoorStatus.Open || d.Status == DoorStatus.Opening)
                : null;

            public void Close()
            {
                Doors.ForEach(d => d.CloseDoor());
            }

            private void Configure()
            {
                string innerVentName = Config.Get(ConfigSectionName, "Inner vent").ToString();
                string outerVentName = Config.Get(ConfigSectionName, "Outer vent").ToString();
                foreach (var button in Buttons)
                {

                    var sb = new StringBuilder();
                    var provider = (button as IMyTextSurfaceProvider);
                    int surfaceNumber = 0;

                    if (!string.IsNullOrWhiteSpace(button.CustomData))
                        continue;

                    var buttonConfig = new MyIni();
                    buttonConfig.TryParse(button.CustomData);
                    if (provider != null && provider.SurfaceCount > 1)
                    {
                        surfaceNumber = buttonConfig.Get(ConfigSectionName, $"Airlock button").ToInt32(0);
                        sb.AppendLine($"@{surfaceNumber} AutoLCD");
                    }

                    sb.AppendLine($"Center <<< {Name} Airlock >>>");
                    sb.AppendLine($"Oxygen {{{Vent.CustomName}}}");
                    Doors.ForEach(d =>
                    {
                        sb.AppendLine($"Working {{{d.CustomName}}}");
                    });
                    sb.AppendLine("Echo");

                    bool workingAdded = false;
                    if (!string.IsNullOrWhiteSpace(innerVentName) && !button.CustomName.Contains("Inner"))
                    {
                        sb.AppendLine($"Oxygen {{{ innerVentName }}}");
                        workingAdded = true;
                    }

                    if (!string.IsNullOrWhiteSpace(outerVentName) && !button.CustomName.Contains("Outer"))
                    {
                        sb.AppendLine($"Oxygen {{{ outerVentName }}}");
                        workingAdded = true;
                    }

                    if (workingAdded)
                    {
                        sb.AppendLine("Echo");
                    }
                    sb.AppendLine("Tanks {Oxygen Tank} Oxygen");
                    buttonConfig.EndContent = sb.ToString();
                    button.CustomData = buttonConfig.ToString();

                    if (!button.CustomName.Contains(LcdTag))
                    {
                        button.CustomName = button.CustomName.Replace("[LCD]", "").TrimEnd() + " " + LcdTag;
                    }

                    if (provider != null)
                    {
                        var textSurface = (button as IMyTextSurfaceProvider).GetSurface(surfaceNumber);
                        textSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                        textSurface.BackgroundColor = Constants.Colors.SurfaceBackground;
                        textSurface.Alignment = TextAlignment.LEFT;
                        textSurface.FontSize = 1f;
                        textSurface.TextPadding = 2f;
                    }
                }
            }

            public bool IsInnerDoor(IMyDoor door)
            {
                return door.CustomName.Contains("Inner");
            }

            public void TogglePressure(bool pressurize)
            {
                if (Enabled)
                {
                    Vent.Depressurize = !pressurize;
                    foreach (var light in Lights)
                    {
                        light.Color = pressurize ? PressurizedColor : DepressurizedColor;
                    }
                }
            }

            public void Activate()
            {
                Doors.ForEach(d => d.Enabled = true);
                Vent.Enabled = true;
                foreach (var b in Buttons)
                {
                    b.ApplyAction("OnOff_Off");
                }
            }
        }
    }
}