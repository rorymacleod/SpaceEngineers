using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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
        private DrillSystem2 System;

        public Program() : base()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;
            Commands.Add("initialize", Initialize);
            Commands.Add("stop", Stop);
            Commands.Add("drill", Drill);
            Commands.Add("reset", Reset);
        }

        private IEnumerator<UpdateFrequency> Initialize()
        {
            Output.AddTextSurfaces("Mining Rig LCD");

            Output.WriteTitle("Mining Rig");
            System = new DrillSystem2(Config, Output, this);
            foreach (var update in System.Initialize())
            {
                yield return update;
            }
        }

        private IEnumerator<UpdateFrequency> Stop()
        {
            Output.WriteTitle("Mining Rig: Stop");
            foreach (var update in System.Stop())
            {
                yield return update;
            }
        }

        private IEnumerator<UpdateFrequency> Drill()
        {
            Output.WriteTitle("Mining Rig: Drill");
            foreach (var update in System.AdvanceDrill())
            {
                yield return update;
            }
        }

        private IEnumerator<UpdateFrequency> Reset()
        {
            Output.WriteTitle("Mining Rig: Reset");
            foreach (var update in System.Reset())
            {
                yield return update;
            }
        }
    }
}
