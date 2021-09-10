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
    partial class Program
    {
        public class ItemMatcher
        {
            public static readonly ItemMatcher MatchAll = new ItemMatcher();

            public readonly Func<MyItemType, bool> IsMatch;

            private ItemMatcher()
            {
                IsMatch = _ => true;
            }

            public ItemMatcher(string itemSpec)
            {
                int sep = itemSpec.IndexOf('/');
                if (sep > 0 && sep <= itemSpec.Length - 2)
                {
                    string type = itemSpec.Substring(0, sep).ToLower();
                    string subtype = itemSpec.Substring(sep + 1).ToLower();
                    IsMatch = i => i.TypeId.Equals(type, StringComparison.OrdinalIgnoreCase) &&
                        i.SubtypeId.Equals(subtype, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    IsMatch = i => i.TypeId.Equals(itemSpec, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }
}
