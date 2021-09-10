using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Program() : base()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;
            Commands.Add("initialize", Initialize);
            Commands.Add("rename-grid", RenameGrid);
            Commands.Add("safe-zone", SetSafeZone);
            Commands.Add("transfer", TransferCargo);
        }

        private IEnumerator<UpdateFrequency> Initialize()
        {
            Me.SetScriptTitle("Tools");
            Output.AddTextSurfaces("Tools");

            yield return Next();
        }

        public IEnumerator<UpdateFrequency> RenameGrid()
        {
            Output.WriteTitle("Tools: Rename Grid");
            yield return Update100();
            string name = Config.Get("Tools", "Grid name").ToString(null);
            if (string.IsNullOrWhiteSpace(name))
            {
                Output.Write("Custom data value \"Grid Name\" is required.");
                yield break;
            }

            if (Me.CubeGrid.CustomName.StartsWith("Small Grid") ||
                Me.CubeGrid.CustomName.StartsWith("Large Grid") ||
                Me.CubeGrid.CustomName.StartsWith("Static Grid"))
            {
                Me.CubeGrid.CustomName = name;
                Output.Write($"Grid renamed to \"{name}\".");
            }
            else
            {
                Output.Write($"Grid already has a name of \"{Me.CubeGrid.CustomName}\".");
            }
        }

        public IEnumerator<UpdateFrequency> SetSafeZone()
        {
            Output.WriteTitle("Tools: Safe Zone");
            if (CommandLine.ArgumentCount == 3)
            {
                var safeZone = new SafeZone(CommandLine.Argument(1), Output, this);
                yield return Next();

                if (CommandLine.Argument(2) == "hi")
                {
                    foreach (var next in safeZone.SetHi())
                    {
                        yield return next;
                    }
                }
                else if (CommandLine.Argument(2) == "lo")
                {
                    foreach (var next in safeZone.SetLo())
                    {
                        yield return next;
                    }
                }
                else
                {
                    Output.Write($"Argument \"{CommandLine.Argument(2)}\" not recognised.");
                }
            }
            else
            {
                Output.Write("Usage: safe-zone \"Safe Zone Name\" (hi|lo)");
            }
        }

        public IEnumerator<UpdateFrequency> TransferCargo()
        {
            Output.WriteTitle("Tools: Transfer Cargo");
            const string usage = "Usage: transfer (load|unload) [ore/stone]*";

            if (CommandLine.ArgumentCount < 2)
            {
                Output.Write(usage);
                yield break;
            }

            bool load;
            switch (CommandLine.Argument(1).ToLower())
            {
                case "load":
                    load = true;
                    break;

                case "unload":
                    load = false;
                    break;

                default:
                    Output.Write($"Unknown argument \"{CommandLine.Argument(0)}\". Expected (load|unload).");
                    Output.Write(usage);
                    yield break;
            }


            var sourceInventories = GetTransferSourceInventories(load);
            var targetInventories = GetTransferTargetInventories(load);
            yield return Next();

            if (sourceInventories.Count == 0)
            {
                Output.Write($"No source inventories found.");
                yield break;
            }
            else if (targetInventories.Count == 0)
            {
                Output.Write("No target inventories found.");
            }
            else
            {
                Output.Write($"{(load ? "Loading" : "Unloading into")} {targetInventories.Count} containers.");
            }
            yield return Next();

            List<ItemMatcher> itemMatchers;
            if (CommandLine.ArgumentCount < 3)
            {
                itemMatchers = new List<ItemMatcher> { ItemMatcher.MatchAll };
            }
            else
            {
                itemMatchers = new List<ItemMatcher>();
                for (int i = 2; i < CommandLine.ArgumentCount; i++)
                {
                    itemMatchers.Add(new ItemMatcher(CommandLine.Argument(i)));
                }
            }
            yield return Next();

            var itemTypes = GetInventoryItemTypes(sourceInventories, itemMatchers);
            if (itemTypes.Count == 0)
            {
                Output.Write("No matching items found in source inventories.");
                yield break;
            }
            yield return Next();

            foreach (var itemType in itemTypes)
            {
                TransferAllAvailable(itemType, sourceInventories, targetInventories);

                if (targetInventories.Count == 0)
                {
                    Output.Write("All target inventories are full.");
                    yield break;
                }

                yield return Next();
            }
        }

        private List<IMyInventory> GetTransferSourceInventories(bool load)
        {
            var sourceBlocks = this.GetBlocksOfType<IMyTerminalBlock>(
                b => load == (b.CubeGrid != Me.CubeGrid) && b.HasInventory && b.HasLocalPlayerAccess() && b.IsWorking);

            return this.GetInventories(sourceBlocks);
        }

        private List<IMyInventory> GetTransferTargetInventories(bool load)
        {
            var targetBlocks = this.GetBlocksOfType<IMyCargoContainer>(
                b => load == (b.CubeGrid == Me.CubeGrid) && b.HasLocalPlayerAccess() && b.IsWorking)
                .OrderBy(b => b.CustomName)
                .ToList();

            return this.GetInventories(targetBlocks);
        }

        private List<MyItemType> GetInventoryItemTypes(List<IMyInventory> sourceInventories,
            List<ItemMatcher> itemMatchers)
        {
            var itemTypes = new HashSet<MyItemType>();
            foreach (var source in sourceInventories)
            {
                var items = new List<MyInventoryItem>();
                source.GetItems(items, i => itemMatchers.Any(m => m.IsMatch(i.Type)));
                foreach (var item in items)
                {
                    itemTypes.Add(item.Type);
                }
            }

            return itemTypes.ToList();
        }

        private void TransferAllAvailable(MyItemType itemType, List<IMyInventory> sourceInventories,
            List<IMyInventory> targetInventories)
        {
            var info = itemType.GetItemInfo();
            string format = $"{{0:{(info.UsesFractions ? "N2" : "N")}}} {(info.UsesFractions ? "kg" : string.Empty)}";
            float total = 0;
            foreach (var source in sourceInventories)
            {
                if (source.ContainItems(0, itemType))
                {
                    total += (float)TransferAllFromSource(source, itemType, targetInventories, format);
                }
            }

            Output.Write($"Transferred {itemType.SubtypeId} x {string.Format(format, total)}.");
            var remaining = sourceInventories.Sum(i => (float)i.GetItemAmount(itemType));
            Output.Write($"{string.Format(format, remaining)} {itemType.SubtypeId} remaining.");
        }

        private MyFixedPoint TransferAllFromSource(IMyInventory source, MyItemType itemType, 
            List<IMyInventory> targetInventories, string format)
        {
            var sourceItems = new List<MyInventoryItem>();
            source.GetItems(sourceItems, i => i.Type == itemType);

            MyFixedPoint total = 0;
            foreach (var sourceItem in sourceItems)
            {
                //Output.Write($"{source.Owner.GetName()} has an item of {sourceItem.Amount}.");
                
                var amount = TransferItemToTargets(source, sourceItem, targetInventories, format);
                total += amount;
                if (targetInventories.Count == 0 || amount < sourceItem.Amount)
                {
                    Output.Write("Not all items were transferred.");
                    break;
                }
            }

            return total;
        }

        private MyFixedPoint TransferItemToTargets(IMyInventory source, MyInventoryItem sourceItem, 
            List<IMyInventory> targetInventories, string format)
        {
            MyFixedPoint totalAmount = 0;
            for (int i = 0; i < targetInventories.Count; i++)
            {
                var target = targetInventories[i];
                if (target.IsFull)
                {
                    Output.Write($"{target.Owner.GetName()} inventory is full.");
                    targetInventories.RemoveAt(i);
                    i--;
                    continue;
                }

                if (source.CanTransferItemTo(target, sourceItem.Type))
                {
                    if (!source.TransferItemTo(target, sourceItem))
                    {
                        Output.Write("TransferItemTo returned false");
                    }

                    var newItem = source.GetItemByID(sourceItem.ItemId);
                    if (newItem == null)
                    {
                        totalAmount += sourceItem.Amount;
                        Output.Write($"Transferred {sourceItem.Type.SubtypeId} x " +
                            $"{string.Format(format, (float)sourceItem.Amount)}");
                        Output.Write($"    from {source.Owner.GetName()}");
                        Output.Write($"    to {target.Owner.GetName()}");
                        Output.Write("Item removed.");
                        break;
                    }

                    var transferredAmount = sourceItem.Amount - newItem.Value.Amount;
                    totalAmount += transferredAmount;
                    sourceItem = newItem.Value;
                    Output.Write($"Transferred {sourceItem.Type.SubtypeId} x " +
                        $"{string.Format(format, (float)transferredAmount)}");
                    Output.Write($"    from {source.Owner.GetName()}");
                    Output.Write($"    to {target.Owner.GetName()}");
                }
                else
                {
                    Output.Write($"Cannot transfer {sourceItem.Type.SubtypeId} from " +
                        $"{source.Owner.GetName()} to {target.Owner.GetName()}");
                }
            }

            return totalAmount;
        }
    }
}
