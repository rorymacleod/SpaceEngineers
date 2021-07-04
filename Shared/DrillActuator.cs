using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        public class DrillActuator
        {
            private List<IMyPistonBase> Pistons;
            private IMyShipWelder Welder;
            private IMyShipConnector Connector;
            private IMyShipMergeBlock MergeBlock;
            private IMyProjector Projector;
            private readonly MyGridProgram Grid;
            private readonly LcdManager Output;

            public string Name { get; }

            public DrillActuator(string name, LcdManager output, MyGridProgram grid)
            {
                Grid = grid;
                Name = name;
                Output = output;
            }

            private bool IsFullyExtended => Pistons.TrueForAll(p => FloatEquals(p.CurrentPosition, 10));
            private bool IsFullyRetracted => Pistons.TrueForAll(p => FloatEquals(p.CurrentPosition, 0));
            public bool Connected => Connector.Status == MyShipConnectorStatus.Connected;
            public string ConnectorName => Connector.CustomName;
            public float PistonPosition => Pistons.Sum(p => p.CurrentPosition);

            public IEnumerable<UpdateFrequency> Initialize()
            {
                Pistons = Grid.FindBlocks<IMyPistonBase>(Name);
                yield return Next();

                Connector = Grid.FindBlock<IMyShipConnector>(Name);
                yield return Next();

                MergeBlock = Grid.FindBlock<IMyShipMergeBlock>(Name);
                yield return Next();
                
                Projector = Grid.FindBlock<IMyProjector>(Name);
                yield return Next();

                var hinges = Grid.FindBlocks<IMyMotorStator>(Name);
                Welder = Grid.FindBlock<IMyShipWelder>(Name);
                yield return Next();
            }

            public void Stop()
            {
                Pistons.ForEach(p => p.Enabled = false);
                Projector.Enabled = false;
                Welder.Enabled = false;
            }

            public IEnumerable<UpdateFrequency> Connect()
            {
                while (Connector.Status != MyShipConnectorStatus.Connected)
                {
                    if (Connector.Status == MyShipConnectorStatus.Connectable)
                    {
                        Connector.Connect();
                        MergeBlock.Enabled = true;
                        Projector.Enabled = true;
                        yield return Update10();
                    }
                    else
                    {
                        yield return Update100();

                    }
                }
            }

            public IEnumerable<UpdateFrequency> Disconnect()
            {
                if (Connector.Status == MyShipConnectorStatus.Connected)
                {
                    Connector.Disconnect();
                    MergeBlock.Enabled = false;
                    Projector.Enabled = false;
                    yield return Update10();
                }
            }

            public IEnumerable<UpdateFrequency> ProjectorOn()
            {
                Projector.Enabled = true;
                yield return Next();
            }

            public IEnumerable<UpdateFrequency> WelderOn()
            {
                Welder.Enabled = true;
                yield return Next();
            }

            public IEnumerable<UpdateFrequency> MoveToEndPosition()
            {
                if (!IsFullyExtended)
                {
                    Output.Write($"Extending {Name} actuator ({PistonPosition:F1}m)...");
                    
                    Extend();
                    while (!IsFullyExtended)
                    {
                        Output.UpdateLine($"Extending {Name} actuator ({PistonPosition:F1}m)...");
                        yield return Update100();
                    }
                    
                    Output.UpdateLine($"Extending {Name} actuator ({PistonPosition:F1}m)...");
                }

                Output.Write($"{Name} actuator in extended position.");
            }

            public IEnumerable<UpdateFrequency> MoveToStartPosition()
            {
                if (!IsFullyRetracted)
                {
                    Output.Write($"Retracting {Name} actuator ({PistonPosition:F1}m)...");
                    
                    Retract();
                    while(!IsFullyRetracted)
                    {
                        Output.UpdateLine($"Retracting {Name} actuator ({PistonPosition:F1}m)...");
                        yield return Update100();
                    }
                    
                    Output.UpdateLine($"Retracting {Name} actuator ({PistonPosition:F1}m)...");
                }

                Output.Write($"{Name} actuator in retracted position.");
            }

            private void Extend()
            {
                foreach (var piston in Pistons)
                {
                    piston.MaxLimit = 10;
                    piston.MinLimit = 0;
                    piston.Velocity = CalculateVelocity() / Pistons.Count;
                    piston.Enabled = true;
                }
            }

            private void Retract()
            {
                foreach (var piston in Pistons)
                {
                    piston.MaxLimit = 10;
                    piston.MinLimit = 0;
                    piston.Velocity = 0 - CalculateVelocity() / Pistons.Count;
                    piston.Enabled = true;
                }
            }

            private float CalculateVelocity()
            {
                return 0.5f;
            }
        }
    }
}
