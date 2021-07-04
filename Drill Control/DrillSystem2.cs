using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Shared;
using System;
using System.Collections;
using System.Collections.Generic;
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
    partial class Program
    {
        public class DrillSystem2
        {
            private const string ConfigSectionName = "Drill Control";

            private readonly MyIni Config;
            private List<DrillActuator> Actuators;
            private readonly DrillHead Head;
            private readonly LcdManager Output;
            private readonly MyGridProgram Grid;

            private DrillActuator ConnectedActuator { get; set; }
            private DrillActuator FreeActuator { get; set; }

            public DrillSystem2(MyIni config, LcdManager output, MyGridProgram grid)
            {
                Config = config;
                Output = output;
                Grid = grid;
            }

            public IEnumerable<UpdateFrequency> Initialize()
            {
                string tag1 = "Left";
                string tag2 = "Right";

                Output.Write($"Initializing {tag1} actuator...");
                var actuator1 = new DrillActuator(tag1, Output, Grid);
                foreach (var update in actuator1.Initialize())
                {
                    yield return update;
                }

                Output.Write($"Initializing {tag2} actuator...");
                var actuator2 = new DrillActuator(tag2, Output, Grid);
                foreach (var update in actuator2.Initialize())
                {
                    yield return update;
                }

                Actuators = new List<DrillActuator> { actuator1, actuator2 };
                ConnectedActuator = Actuators.First(x => x.Connected);
                FreeActuator = Actuators.First(x => x != ConnectedActuator);
                Output.Write($"Connected to {ConnectedActuator.Name} actuator.");
            }

            public IEnumerable<UpdateFrequency> Stop()
            {
                foreach (var actuator in Actuators)
                {
                    actuator.Stop();
                    Output.Write($"Stopped {actuator.Name} actuator.");
                    yield return Next();
                }
            }

            public IEnumerable<UpdateFrequency> AdvanceDrill()
            {
                Output.Write($"Drill is connected to {ConnectedActuator.Name} connector.");

                foreach (var update in Enumerate(
                    ConnectedActuator.MoveToStartPosition,
                    ConnectedActuator.ProjectorOn,
                    WelderOn,
                    SwapConnection
                ))
                {
                    yield return update;
                }
            }

            public IEnumerable<UpdateFrequency> Reset()
            {
                foreach (var update in Enumerate(
                    ConnectedActuator.MoveToStartPosition,
                    FreeActuator.MoveToStartPosition,
                    Stop
                ))
                {
                    yield return update;
                }
            }

            public IEnumerable<UpdateFrequency> SwapConnection()
            {
                FreeActuator.Connect();
                while (!FreeActuator.Connected)
                {
                    yield return Update10();
                }
                Output.Write($"{FreeActuator.ConnectorName} connected.");

                ConnectedActuator.Disconnect();
                while (ConnectedActuator.Connected)
                {
                    yield return Update10();
                }
                Output.Write($"{ConnectedActuator.ConnectorName} disconnected.");

                var connected = FreeActuator;
                var free = ConnectedActuator;
                ConnectedActuator = connected;
                FreeActuator = free;
            }
        }

        public class DrillHead
        {
            private IMyMotorStator Rotor;
            private List<IMyShipDrill> Drills;

            public void Stop()
            {
                Rotor.Enabled = false;
                Drills.ForEach(d => d.Enabled = false);
            }
        }
    }
}
