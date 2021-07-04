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
    partial class Program : MyGridProgram
    {
        public Program() : base()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;
            Commands.Add("rename-grid", RenameGrid);
        }

        public IEnumerator<UpdateFrequency> RenameGrid()
        {
            Output.WriteTitle("Tools: Rename Grid");
            yield return Update100();
            string name = Config.Get("Tools", "Grid name").ToString(null);
            if (string.IsNullOrWhiteSpace(name))
            {
                Output.Write("Custom data value \"Grid Name\" is required.");
                yield break;
            }

            if (Me.CubeGrid.CustomName.StartsWith("Small Grid") ||
                Me.CubeGrid.CustomName.StartsWith("Large Grid") ||
                Me.CubeGrid.CustomName.StartsWith("Static Grid"))
            {
                Me.CubeGrid.CustomName = name;
                Output.Write($"Grid renamed to \"{name}\".");
            }
            else
            {
                Output.Write($"Grid already has a name of \"{Me.CubeGrid.CustomName}\".");
            }
        }
    }
}
