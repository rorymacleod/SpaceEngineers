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
using System.Net;

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

        public static List<T> GetBlocks<T>(this MyGridProgram grid, string name) where T : class, IMyTerminalBlock
        {
            var blocks = new List<IMyTerminalBlock>();
            grid.GridTerminalSystem.SearchBlocksOfName(name, blocks, b => b is T);
            if (blocks.Count == 0)
            {
                throw new Exception($"No blocks found with name '{name}' of type '{typeof(T).Name}'.");
            }

            return blocks.Cast<T>().ToList();
        }

        public static List<T> GetBlocksOfType<T>(this MyGridProgram grid,
            Func<T, bool> predicate = null, bool required = false) where T : class, IMyTerminalBlock
        {
            var blocks = new List<IMyTerminalBlock>();
            grid.GridTerminalSystem.GetBlocksOfType<T>(blocks, b => predicate == null || predicate((T)b));
            if (required && blocks.Count == 0)
            {
                throw new Exception($"No blocks found of type '{typeof(T).Name}'.");
            }

            return blocks.Cast<T>().ToList();
        }

        public static List<IMyInventory> GetInventories(this MyGridProgram grid, IEnumerable<IMyTerminalBlock> blocks = null)
        {
            if (blocks == null)
            {
                var list = new List<IMyTerminalBlock>();
                grid.GridTerminalSystem.GetBlocks(list);
                blocks = list;
            }

            var inventories = new List<IMyInventory>();
            foreach (var block in blocks)
            {
                if (block.HasInventory)
                {
                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        inventories.Add(block.GetInventory(i));
                    }
                }
            }

            return inventories;
        }

        public static List<MyInventoryItem> GetInventoryItems(this MyGridProgram grid, IEnumerable<IMyInventory> inventories, string typeId, string subtypeId)
        {
            var items = new List<MyInventoryItem>();
            foreach (var inventory in inventories)
            {
                inventory.GetItems(items, i => i.Type.TypeId == typeId && i.Type.SubtypeId == subtypeId);
            }
            return items;
        }

        public static List<T> FindBlocks<T>(this MyGridProgram grid, string name) where T : class, IMyTerminalBlock
        {
            var blocks = new List<IMyTerminalBlock>();
            grid.GridTerminalSystem.SearchBlocksOfName(name, blocks, b => b is T);

            return blocks.OfType<T>().ToList();
        }

        public static T FindBlock<T>(this MyGridProgram grid, string name) where T : class, IMyTerminalBlock
        {
            var list = FindBlocks<T>(grid, name);
            if (list.Count == 0)
            {
                throw new Exception($"No blocks found with name '{name}' of type '{typeof(T).Name}'.");
            }

            return list[0];
        }

        public static List<T> GetGroupBlocks<T>(this MyGridProgram grid, string groupName) where T : class, IMyTerminalBlock
        {
            var group = grid.GridTerminalSystem.GetBlockGroupWithName(groupName);
            if (group == null)
            {
                throw new Exception($"No group found with name '{groupName}'.");
            }

            var blocks = new List<T>();
            group.GetBlocksOfType(blocks);
            if (blocks.Count == 0)
            {
                throw new Exception($"The group '{groupName}' does not contain any blocks of type '{typeof(T).Name}'.");
            }

            return blocks;
        }

        public static void InitConfiguration(this MyGridProgram grid, MyIni configuration)
        {
            InitConfiguration(grid.Me.CustomData, configuration);
        }

        public static void InitConfiguration(string customData, MyIni configuration)
        {
            MyIniParseResult config;
            if (!configuration.TryParse(customData, out config))
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

        public static IEnumerator<UpdateFrequency> RunOperation(this MyGridProgram grid, 
            IEnumerator<UpdateFrequency> operation)
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

        public static void SetScriptTitle(this IMyProgrammableBlock block, string title)
        {
            var provider = block as IMyTextSurfaceProvider;
            var main = provider.GetSurface(0);
            var keyboard = provider.GetSurface(1);
            main.ContentType = ContentType.TEXT_AND_IMAGE;
            main.FontColor = new Color(0, 220, 0);
            keyboard.ContentType = ContentType.TEXT_AND_IMAGE;
            keyboard.FontColor = main.FontColor;
            keyboard.FontSize = 5f;
            keyboard.Alignment = TextAlignment.CENTER;
            keyboard.TextPadding = 30f;
            keyboard.WriteText(title);
        }

        public static string GetName(this IMyEntity entity)
        {
            if (entity is IMyTerminalBlock)
            {
                return ((IMyTerminalBlock)entity).CustomName;
            }

            if (entity is IMyCubeBlock)
            {
                return ((IMyCubeBlock)entity).DisplayNameText;
            }

            return entity.GetType().Name;
        }
    }
}
