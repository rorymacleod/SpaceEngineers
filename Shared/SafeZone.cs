using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class SafeZone
        {
            private readonly IMySafeZoneBlock Block;
            private readonly MyGridProgram Grid;
            private readonly LcdManager Output;

            public SafeZone(string name, LcdManager output, MyGridProgram grid)
            {
                Block = grid.GetBlocks<IMySafeZoneBlock>(name).First();
                Grid = grid;
                Output = output;
            }

            public bool IsSpherical
            {
                get { return Block.GetValue<long>("SafeZoneShapeCombo") == 0; }
                set { Block.SetValue<long>("SafeZoneShapeCombo", value ? 0 : 1); }
            }

            public float Size
            {
                get { return Block.GetValue<float>("SafeZoneSlider"); }
                set { Block.SetValue("SafeZoneSlider", value); }
            }

            public float SizeX
            {
                get { return Block.GetValue<float>("SafeZoneXSlider"); }
                set { Block.SetValue("SafeZoneXSlider", value); }
            }

            public float SizeY
            {
                get { return Block.GetValue<float>("SafeZoneYSlider"); }
                set { Block.SetValue("SafeZoneYSlider", value); }
            }

            public float SizeZ
            {
                get { return Block.GetValue<float>("SafeZoneZSlider"); }
                set { Block.SetValue("SafeZoneZSlider", value); }
            }

            public bool ZoneEnabled
            {
                get { return Block.GetValue<bool>("SafeZoneCreate"); }
                set 
                {
                    bool current = Block.GetValue<bool>("SafeZoneCreate");
                    if (current != value)
                    {
                        Block.SetValue("SafeZoneCreate", true); 
                    }
                }
            }

            public IEnumerable<UpdateFrequency> SetHi()
            {
                var config = ReadConfiguration(Block.CustomData);
                if (config == null)
                {
                    throw new Exception("Safe zone custom data is missing.");
                }

                string section = $"Safe Zone (Hi)";
                return SetFromConfig(config, section, true);
            }

            public IEnumerable<UpdateFrequency> SetLo()
            {
                var config = ReadConfiguration(Block.CustomData);
                if (config == null)
                {
                    throw new Exception("Safe zone custom data is missing.");
                }

                string section = $"Safe Zone (Lo)";
                return SetFromConfig(config, section, false);
            }

            private IEnumerable<UpdateFrequency> SetFromConfig(MyIni config, string section, bool enableReactor)
            {
                bool spherical = config.Get(section, "Spherical").ToBoolean(true);
                if (spherical)
                {
                IsSpherical = true;
                    Size = config.Get(section, "Size").ToSingle();
                    yield return Next();

                    Output.Write($"{Block.CustomName} set to {Size}m");
                }
                else
                {
                    IsSpherical = false;
                    SizeX = config.Get(section, "SizeX").ToSingle();
                    SizeY = config.Get(section, "SizeY").ToSingle();
                    SizeZ = config.Get(section, "SizeZ").ToSingle();
                    yield return Next();

                    Output.Write($"{Block.CustomName} set to {SizeX}x{SizeY}x{SizeZ}m");
                }

                string reactorName = config.Get(section, "Reactor").ToString();
                if (!string.IsNullOrEmpty(reactorName))
                {
                    var reactor = Grid.GetBlock<IMyReactor>(reactorName);
                    reactor.Enabled = enableReactor;
                    Output.Write($"{reactor.CustomName} {(enableReactor ? "enabled" : "disabled")}.");
                    yield return Next();
                }

                ZoneEnabled = true;
            }
        }
    }
}
