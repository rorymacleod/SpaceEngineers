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
        private const string ConfigSectionName = "Container Control";
        private List<IMyInventory> Inventories;
        private IMyTimerBlock FullTimer;
        private float FullLevel;
        private bool FullTriggered;
        private IMyTimerBlock SpaceTimer;
        private float SpaceLevel;
        private bool SpaceTriggered;

        private IEnumerator<UpdateFrequency> Initialize()
        {
            if (Initialized) yield break;
            foreach (var update in Enumerate(InitializeCommon()))
            {
                yield return update;
            }

            Commands["check"] = CheckFillLevel;
            AutoRunCommand = "check";

            Output.WriteTitle("Container Control");
            string groupName = Config.Get(ConfigSectionName, "Cargo Group").ToString();
            if (string.IsNullOrWhiteSpace(groupName))
                throw new Exception($"'Cargo Group' in config section '{ConfigSectionName}' is required.");

            var groupBlocks = this.GetGroupBlocks<IMyTerminalBlock>(groupName);
            Inventories = this.GetInventories(groupBlocks);
            Output.Write($"Found {Inventories.Count} inventory blocks in group \"{groupName}\".");
            yield return UpdateFrequency.Once;

            var fullTimerName = Config.Get(ConfigSectionName, "Full Timer").ToString();
            if (!string.IsNullOrWhiteSpace(fullTimerName))
            {
                FullTimer = this.GetBlock<IMyTimerBlock>(fullTimerName);
                FullLevel = Config.Get(ConfigSectionName, "Full Level").ToSingle(0.95f);
                Output.Write($"Start \"{FullTimer.CustomName}\" at {FullLevel:p0}%");
                yield return UpdateFrequency.Once;
            }

            var spaceTimerName = Config.Get(ConfigSectionName, "Space Timer").ToString();
            if (!string.IsNullOrWhiteSpace(spaceTimerName))
            {
                SpaceTimer = this.GetBlock<IMyTimerBlock>(spaceTimerName);
                SpaceLevel = Config.Get(ConfigSectionName, "Space Level").ToSingle(0.5f);
                Output.Write($"Start \"{SpaceTimer.CustomName}\" at {SpaceLevel:p0}%");
            }
        }

        private IEnumerator<UpdateFrequency> CheckFillLevel()
        {
            float capacity = 0;
            float level = 0;
            foreach (var inventory in Inventories)
            {
                capacity += inventory.MaxVolume.RawValue;
                level += inventory.CurrentVolume.RawValue;
            }

            float currentLevel = level / capacity;
            Output.WriteTitle("Container Control");
            Output.Write($"Current level: {currentLevel:P1}");
            yield return UpdateFrequency.Once;

            if (FullTimer != null && currentLevel >= FullLevel && !FullTriggered)
            {
                FullTriggered = true;
                FullTimer.StartCountdown();
                yield return UpdateFrequency.Once;
            }
            else if (currentLevel < FullLevel)
            {
                FullTriggered = false;
            }

            if (SpaceTimer != null && currentLevel <= SpaceLevel && !SpaceTriggered)
            {
                SpaceTriggered = true;
                SpaceTimer.StartCountdown();
                yield return UpdateFrequency.Once;
            }
            else if (currentLevel > SpaceLevel)
            {
                SpaceTriggered = false;
            }

            if (FullTriggered)
            {
                Output.Write($"Starting {FullTimer.CustomName} at {FullLevel:P0}%");
            }

            if (SpaceTriggered)
            {
                Output.Write($"Starting {SpaceTimer.CustomName} at {SpaceLevel:P0}%");
            }

            yield return UpdateFrequency.Update100;
        }
    }
}
