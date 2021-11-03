using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
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
using Shared;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private const string ConfigSectionName = "Threat Detection";

        private List<IMyLargeGatlingTurret> Turrets;
        private IMyTimerBlock AlertTimer;
        private IMyTimerBlock StandbyTimer;
        private bool IsAlerting;

        public Program() : base()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;
            Commands.Add("initialize", Initialize);
            Commands.Add("detect", Detect);
            AutoRunCommand = "detect";
        }

        private IEnumerator<UpdateFrequency> Initialize()
        {
            Me.SetScriptTitle("Threat");
            Output.AddTextSurfaces("Threat");
            Output.WriteTitle("Threat Detection");

            Turrets = this.GetBlocksOfType<IMyLargeGatlingTurret>(t => t.Enabled);
            Output.Write($"Found {Turrets.Count} guns...");
            yield return Next();

            AlertTimer = this.GetBlock<IMyTimerBlock>(Config.GetRequiredString(ConfigSectionName, "Alert timer"));
            Output.Write($"On alert, trigger {AlertTimer.CustomName}.");
            yield return Next();

            StandbyTimer = this.GetBlock<IMyTimerBlock>(Config.GetRequiredString(ConfigSectionName, "Standby timer"));
            Output.Write($"On standby, start {StandbyTimer.CustomName}.");
            yield return Next();
        }

        public IEnumerator<UpdateFrequency> Detect()
        {
            Output.Write("Starting threat detection...");
            DateTime cooldownTime = DateTime.MinValue;

            while (true)
            {
                yield return Update100();

                var shooting = Turrets.FirstOrDefault(t => t.IsWorking && t.Enabled && t.HasTarget);
                if (shooting != null && !IsAlerting)
                {
                    var target = shooting.GetTargetedEntity();
                    var speed = target.Velocity.Length();
                    if (target.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                    cooldownTime = DateTime.Now.AddSeconds(10);
                    Output.WriteTitle("Threat: Detect");
                    Output.Write($"ALERT: {shooting.CustomName}");
                    Output.Write("    is targeting");
                    Output.Write($"    {target.Name}.");
                    Output.Write($"    ({target.Relationship}, {speed:f2} m/s)");
                    if (target.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies && speed > 0)
                    {
                        IsAlerting = true;
                        AlertTimer.Trigger();
                        StandbyTimer.StopCountdown();
                        Output.Write($"Triggered {AlertTimer.CustomName}");
                        yield break;
                    }
                }

                if (IsAlerting && DateTime.Now > cooldownTime)
                {
                    IsAlerting = false;
                    cooldownTime = DateTime.MinValue;
                    StandbyTimer.StartCountdown();
                    Output.Write($"Started {StandbyTimer.CustomName}");
                    Output.Write("Resuming threat detection...");
                }
            }
        }
    }
}
