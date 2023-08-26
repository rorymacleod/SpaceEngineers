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
        public class DrillSystem
        {
            private const string ConfigSectionName = "Drill Control";

            private readonly MyIni Config;
            private List<DrillActuator> Actuators;
            private DrillHead Head;
            private readonly LcdManager Output;
            private readonly MyGridProgram Grid;

            private DrillActuator ConnectedActuator { get; set; }
            private DrillActuator FreeActuator { get; set; }

            public DrillSystem(MyIni config, LcdManager output, MyGridProgram grid)
            {
                Config = config;
                Output = output;
                Grid = grid;
            }

            public IEnumerable<UpdateFrequency> Initialize()
            {
                string tag1 = "Fwd";
                string tag2 = "Aft";

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

                Head = new DrillHead(Output, Grid);
                foreach (var update in Head.Initialize())
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
                Head.Stop();
                Output.Write("Stopped drill head.");
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
                    FreeActuator.WelderOn,
                    FreeActuator.MoveToEndPosition
                ))
                {
                    yield return update;
                }

                //foreach (var update in SwapConnection())
                //{
                //    yield return update;
                //}
                //Output.Write("Handoff complete.");
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

            //public IEnumerator<UpdateFrequency> Withdraw()
            //{
            //    if (!ConnectedActuator.IsWithdrawn)
            //    {
            //        Output.Write("Withdrawing connected actuator...");
            //        ConnectedActuator.Withdraw();
            //        yield return Update1();
            //    }

            //    if (!FreeActuator.IsAdvanced)
            //    {
            //        Output.Write("Advancing connected actuator...");
            //        FreeActuator.Advance();
            //        yield return Update1();
            //    }

            //    while (!ConnectedActuator.IsWithdrawn || !FreeActuator.IsAdvanced)
            //    {
            //        yield return Update100();
            //    }

            //    Output.Write("Actuators in handoff position.");
            //    ConnectedActuator.Stop();
            //    FreeActuator.Stop();
            //    foreach (var update in SwapConnection())
            //    {
            //        yield return update;
            //    }
            //    Output.Write("Handoff complete.");
            //}

            public IEnumerable<UpdateFrequency> SwapConnection()
            {
                Output.Write($"Handoff drill to {FreeActuator.Name} actuator...");
                foreach (var update in Enumerate(
                    FreeActuator.Connect,
                    ConnectedActuator.Disconnect
                ))
                {
                    yield return update;
                }

                var connected = FreeActuator;
                FreeActuator = ConnectedActuator;
                ConnectedActuator = connected;
                
                Output.Write($"Connected to {ConnectedActuator.Name} actuator.");
                yield return Next();
            }

            public IEnumerable<UpdateFrequency> ToggleDrills()
            {
                if (Head.Running)
                {
                    Head.Stop();
                    Output.Write("Stopped drill head.");
                }
                else
                {
                    Head.Start();
                    Output.Write("Started drill head.");
                }
                yield return Next();
            }
        }
    }
}
