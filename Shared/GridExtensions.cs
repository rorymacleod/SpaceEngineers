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
using System.Diagnostics;

namespace IngameScript
{
    static class GridExtensions
    {
        public static void Each<T>(this MyGridProgram grid, IList<T> blocks, Action<T> action) where T: class, IMyTerminalBlock
        {
            foreach (var b in blocks)
            {
                action(b);
            }
        }

        public static T GetBlock<T>(this MyGridProgram grid, string name) where T : class, IMyTerminalBlock
        {
            var block = grid.GridTerminalSystem.GetBlockWithName(name);
            if (block == null)
            {
                throw new Exception($"Block '{name}' not found.");
            }

            if (!(block is T))
            {
                throw new Exception($"Block '{name}' is not of type '{typeof(T).Name}'.");
            }

            return (T)block;
        }

        public static IList<T> GetBlocks<T>(this MyGridProgram grid, string name) where T : class, IMyTerminalBlock
        {
            var blocks = new List<IMyTerminalBlock>();
            grid.GridTerminalSystem.SearchBlocksOfName(name, blocks, b => b is T);
            if (blocks.Count == 0)
            {
                throw new Exception($"No blocks found with name '{name}' of type '{typeof(T).Name}'");
            }

            return blocks.Cast<T>().ToList();
        }

        public static void InitConfiguration(this MyGridProgram grid, MyIni configuration)
        {
            MyIniParseResult config;
            if (!configuration.TryParse(grid.Me.CustomData, out config))
            {
                throw new Exception(config.ToString());
            }
        }

        public static Action<string> InitDebug(this MyGridProgram grid)
        {
            var next = grid.Echo;
            var pb = (IMyProgrammableBlock)grid.Me;
            var pbDisplay = pb.GetSurface(0);
            return s =>
            {
                pbDisplay.WriteText(s, true);
                next(s);
            };
        }

        public static bool IsCommand(this MyGridProgram _, UpdateType updateSource)
            => (updateSource & (UpdateType.Trigger | UpdateType.Terminal | UpdateType.Script)) != 0;

        public static IEnumerator<UpdateFrequency> RunOperation(this MyGridProgram grid, IEnumerator<UpdateFrequency> operation)
        {
            if (operation != null)
            {
                if (operation.MoveNext())
                {
                    grid.Runtime.UpdateFrequency |= operation.Current;
                }
                else
                {
                    operation.Dispose();
                    return null;
                }
            }

            return operation;
        }
    }
}
