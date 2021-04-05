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
using Sandbox.Game.GUI;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private const string ConfigSectionName = "Drill Control";
        private const float SurfaceLevel = 7.3f;
        private const float SensorMargin = 4.1f;
        private const decimal MaxCargoFillLevel = 0.95m;

        private IList<IMyPistonBase> DrillPistons;
        private IList<IMyShipDrill> Drills;
        private IMyLandingGear DrillDockingGear;
        private IList<IMyDoor> DrillDoors;
        private IList<IMyInteriorLight> DrillLights;
        private IMySensorBlock DrillSensor;
        private IMyFunctionalBlock DrillSorter;
        private IList<IMyFunctionalBlock> EjectorBlocks;
        private IList<IMyCargoContainer> CargoContainers;
        private float DrillingVelocity;
        private float MovementVelocity;
        private float DoorClearanceDistance = 5;
        private float TargetDepth;
        private float DetectedSurface = 0;
        private bool Drilling = false;

        public Program()
        {
            Echo = this.InitDebug();
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        private IEnumerator<UpdateFrequency> Initialize()
        {
            if (Initialized) yield break;
            foreach (var update in Enumerate(InitializeCommon()))
            {
                yield return update;
            }

            string prefix = Config.Get(ConfigSectionName, "Prefix").ToString();
            Output.AddTextSurfaces("Drill Control");
            Output.WriteTitle("Drill Control");
            Output.Write($"Found {Output.Surfaces.Count - 1} output text panels.");
            yield return UpdateFrequency.Once;

            Drills = this.GetBlocks<IMyShipDrill>($"{prefix} Drill");
            Output.Write($"Found {Drills.Count} drills.");
            yield return UpdateFrequency.Once;

            DrillPistons = this.GetBlocks<IMyPistonBase>($"{prefix} Drill Piston");
            Output.Write($"Found {DrillPistons.Count} pistons doors.");
            yield return UpdateFrequency.Once;

            DrillDoors = this.GetBlocks<IMyDoor>($"{prefix} Drill Door");
            Output.Write($"Found {DrillDoors.Count} doors.");
            yield return UpdateFrequency.Once;

            DrillLights = this.GetBlocks<IMyInteriorLight>($"{prefix} Drill Light");
            Output.Write($"Found {DrillLights.Count} lights.");
            yield return UpdateFrequency.Once;

            EjectorBlocks = this.GetBlocks<IMyFunctionalBlock>($"{prefix} Ejector");
            Output.Write($"Found {EjectorBlocks.Count} ejector system blocks.");
            yield return UpdateFrequency.Once;

            CargoContainers = this.GetBlocks<IMyCargoContainer>(
                Config.Get(ConfigSectionName, "CargoContainerName").ToString());
            Output.Write($"Found {CargoContainers.Count} cargo containers.");
            yield return UpdateFrequency.Once;

            if (!string.IsNullOrEmpty(Storage))
            {
                var data = new MyIni();
                data.TryParse(Storage);
                DetectedSurface = (float)data.Get(ConfigSectionName, "DetectedSurface").ToDecimal();
                Output.Write($"Detected surface level: {Math.Round(DetectedSurface, 1)}m.");
            }

            DrillDockingGear = this.GetBlock<IMyLandingGear>($"{prefix} Drill Docking Gear");
            DrillSensor = this.GetBlock<IMySensorBlock>($"{prefix} Drill Position Sensor");
            DrillSorter = this.GetBlock<IMyFunctionalBlock>($"{prefix} Drill Output Sorter");
            TargetDepth = (float)Config.Get(ConfigSectionName, "TargetDepth").ToDecimal(20);
            Output.Write($"Target depth: {TargetDepth}m");
            DrillingVelocity = (float)Config.Get(ConfigSectionName, "DrillingVelocity").ToDecimal(0.03m);
            MovementVelocity = (float)Config.Get(ConfigSectionName, "MovementVelocity").ToDecimal(0.5m);
            Initialized = true;
        }

        private IEnumerator<UpdateFrequency> StartDrill()
        {
            this.InitConfiguration(Config);
            TargetDepth = (float)Config.Get(ConfigSectionName, "TargetDepth").ToDecimal(20);
            yield return UpdateFrequency.Once;

            this.Each(DrillLights, l => l.Enabled = true);            
            if (DrillDoors.Any(d => d.Status != DoorStatus.Open))
            {
                Output.WriteTitle("Drill Start");
                Output.Write("Opening doors...");
                this.Each(DrillDoors, d => d.OpenDoor());
                yield return UpdateFrequency.Once;

                while (DrillDoors.Any(d => d.Status != DoorStatus.Open))
                {
                    yield return UpdateFrequency.Update10;
                }

                Output.Write("Doors open.");
            }

            if (DrillPistons.Sum(d => d.CurrentPosition) < DoorClearanceDistance)
            {
                Output.Write($"Moving to staging position {DoorClearanceDistance}m...");
                DrillDockingGear.Unlock();
                yield return UpdateFrequency.Once;

                this.Each(DrillPistons, p =>
                {
                    p.Velocity = MovementVelocity / DrillPistons.Count;
                    p.MinLimit = 0;
                    p.MaxLimit = DoorClearanceDistance / DrillPistons.Count;
                    p.Enabled = true;
                });
                yield return UpdateFrequency.Once;

                while (DrillPistons.Sum(d => d.CurrentPosition) < DoorClearanceDistance - 0.1)
                {
                    yield return UpdateFrequency.Update100;
                }

                Output.Write($"Moving to surface level {SurfaceLevel}m...");
            }

            this.Each(Drills, d => d.Enabled = true);
            this.Each(DrillLights, l => l.Enabled = true);
            this.Each(EjectorBlocks, b => b.Enabled = true);
            DrillSorter.Enabled = true;
            Output.Write("Drills running.");
            yield return UpdateFrequency.Once;

            SetDrillDepth((DetectedSurface == 0 ? SurfaceLevel : DetectedSurface) + TargetDepth);
            this.Each(DrillPistons, p =>
            {
                p.Velocity = MovementVelocity / DrillPistons.Count;
                p.Enabled = true;
            });
            DrillSensor.Enabled = true;

            if (DetectedSurface == 0)
            {
                while (DetectedSurface == 0 && Drills.All(d => d.GetInventory(0).CurrentMass == 0))
                {
                    yield return UpdateFrequency.Update100;
                }
                float currentDepth = DrillPistons.Sum(p => p.CurrentPosition);
                Output.Write($"Surface level detected at {Math.Round(currentDepth, 1)}m.");
                DetectedSurface = currentDepth;
                SetDrillDepth(DetectedSurface + TargetDepth);
            }

            this.Each(DrillPistons, p =>
            {
                p.Velocity = DrillingVelocity / DrillPistons.Count;
                p.Enabled = true;
            });

            Drilling = true;
        }

        private IEnumerator<UpdateFrequency> StopDrill() 
        {
            this.Each(DrillPistons, p => p.Enabled = false);
            this.Each(Drills, d => d.Enabled = false);
            DrillSensor.Enabled = false;
            Output.Write($"Drilling stopped at {Math.Round(DrillPistons.Sum(p => p.CurrentPosition), 1)}m.");
            Drilling = false;
            yield break;
        }

        private IEnumerator<UpdateFrequency> PauseForCapacity()
        {
            this.Each(DrillPistons, p => p.Enabled = false);
            this.Each(Drills, d => d.Enabled = false);
            DrillSensor.Enabled = false;
            Output.Write($"Drilling paused at {Math.Round(DrillPistons.Sum(p => p.CurrentPosition), 1)}m.");

            while (GetCargoFillLevel() >= MaxCargoFillLevel - 10)
            {
                yield return UpdateFrequency.Update100;
            }

            //DrillOperation = StartDrill();
            yield break;
        }

        private IEnumerator<UpdateFrequency> ReturnDrill()
        {
            Output.WriteTitle("Drill Return");
            this.Each(Drills, d => d.Enabled = false);
            Output.Write("Drills stopped.");
            yield return UpdateFrequency.Once;

            if (DrillPistons.Any(p => p.CurrentPosition != 0))
            {
                Output.Write("Returning drill to start position...");
                this.Each(DrillPistons, p =>
                {
                    p.Velocity = 0 - (MovementVelocity / 6);
                    p.MinLimit = 0;
                    p.MaxLimit = 10;
                    p.Enabled = true;
                });
                yield return UpdateFrequency.Once;

                while (DrillPistons.Any(p => p.CurrentPosition != 0))
                {
                    yield return UpdateFrequency.Update100;
                }

                Output.Write("Drill at start position.");
            }

            if (DrillDoors.Any(d => d.Status != DoorStatus.Closed))
            {
                Output.Write("Closing doors...");
                this.Each(DrillDoors, d => d.CloseDoor());
                yield return UpdateFrequency.Once;

                while (DrillDoors.Any(d => d.Status != DoorStatus.Closed))
                {
                    yield return UpdateFrequency.Update100;
                }
            }

            this.Each(DrillPistons, p => p.Enabled = false);
            this.Each(Drills, d => d.Enabled = false);
            this.Each(DrillLights, l => l.Enabled = false);
            this.Each(EjectorBlocks, b => b.Enabled = false);
            DrillSensor.Enabled = false;
            DrillSorter.Enabled = false;
            DrillDockingGear.Lock();
            DetectedSurface = 0;
            Output.Write("Drilling stopped.");
        }

        private void SetDrillDepth(float target)
        {
            Output.Write($"Drilling to {Math.Round(target, 1)}m.");
            this.Each(DrillPistons, p => p.MaxLimit = target + 0.1f);
            DrillSensor.BottomExtend = (target / DrillPistons.Count) + SensorMargin;
        }

        private decimal GetCargoFillLevel()
        {
            var fillLevel = CargoContainers
               .Select(cc => cc.GetInventory())
               .Average(i => i.CurrentVolume.RawValue / (decimal)i.MaxVolume.RawValue);
            return fillLevel;
        }

        private bool CheckCargoCapacity()
        {
            if (GetCargoFillLevel() >= MaxCargoFillLevel)
            {
                Output.Write($"Cargo fill level at {Math.Round(MaxCargoFillLevel * 100)}%.");
                return false;
            }
            return true;
        }

        public void Save()
        {
            var data = new MyIni();
            data.Set(ConfigSectionName, "DetectedSurface", DetectedSurface);
            Storage = data.ToString();
        }

        //public void Main(string argument, UpdateType updateSource)
        //{
        //    if (DrillOperation == null && Drilling)
        //    {
        //        if (CheckCargoCapacity())
        //        {
        //            Runtime.UpdateFrequency = Runtime.UpdateFrequency | UpdateFrequency.Update100;
        //        }
        //        else
        //        {
        //            DrillOperation = PauseForCapacity();
        //            Runtime.UpdateFrequency = Runtime.UpdateFrequency | UpdateFrequency.Once;
        //        }
        //    }
        //}
    }
}
