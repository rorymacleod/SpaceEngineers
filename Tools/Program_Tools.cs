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
            Commands.Add("initialize", Initialize);
            Commands.Add("rename-grid", RenameGrid);
            Commands.Add("safe-zone", SetSafeZone);
        }

        private IEnumerator<UpdateFrequency> Initialize()
        {
            Me.SetScriptTitle("Tools");
            yield return Next();
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

        public IEnumerator<UpdateFrequency> SetSafeZone()
        {
            Output.WriteTitle("Tools: Safe Zone");
            if (Arguments.Count >= 2)
            {
                var safeZone = new SafeZone(Arguments[0], Output, this);
                yield return Next();

                if (Arguments[1] == "hi")
                {
                    foreach (var next in safeZone.SetHi())
                    {
                        yield return next;
                    }
                }
                else if (Arguments[1] == "lo")
                {
                    foreach (var next in safeZone.SetLo())
                    {
                        yield return next;
                    }
                }
                else
                {
                    Output.Write($"Argument \"{Arguments[1]}\" not recognised.");
                }
            }
            else
            {
                Output.Write("Usage: safe-zone \"Safe Zone Name\" [hi|lo]");
            }
        }
    }
}
