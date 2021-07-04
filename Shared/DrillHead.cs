using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        public class DrillHead
        {
            private const string Name = "Drill Head";
            private readonly MyGridProgram Grid;
            private readonly LcdManager Output;
            private IMyMotorStator Rotor;
            private List<IMyShipDrill> Drills;
            private List<IMyLightingBlock> Lights;

            public bool Running => Rotor.Enabled;

            public DrillHead(LcdManager output, MyGridProgram grid)
            {
                Grid = grid;
                Output = output;
            }

            public IEnumerable<UpdateFrequency> Initialize()
            {
                Rotor = Grid.FindBlock<IMyMotorStator>(Name);
                yield return Next();

                Drills = Grid.GetBlocksOfType<IMyShipDrill>(d => d.CubeGrid == Rotor.TopGrid, true);
                Output.Write($"Found {Drills.Count} drills attached to {Rotor.CustomName}.");
                yield return Next();

                Lights = Grid.FindBlocks<IMyLightingBlock>(Name);
            }

            public void Stop()
            {
                Rotor.Enabled = false;
                Drills.ForEach(d => d.Enabled = false);
            }

            public void Start()
            {
                Rotor.LowerLimitDeg = float.MinValue;
                Rotor.UpperLimitDeg = float.MaxValue;
                Rotor.RotorLock = false;
                Rotor.Enabled = true;
                Drills.ForEach(d => d.Enabled = true);
                Lights.ForEach(d => d.Enabled = true);
            }
        }
    }
}
