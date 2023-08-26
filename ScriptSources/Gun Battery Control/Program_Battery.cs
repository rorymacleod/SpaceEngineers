using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private IMyBeacon Beacon;
        private List<IMyInteriorLight> Lights;
        private List<IMyLargeGatlingTurret> Guns;
        private IMyBatteryBlock Battery;
        private List<IMyInventory> Inventories;

        public Program() : base()
        {
            Commands.Add("initialize", Initialize);
            Commands.Add("update", UpdateBatteryStatus);
        }

        private IEnumerator<UpdateFrequency> Initialize()
        {
            Output.WriteTitle("Gun Battery Control");
            Beacon = this.GetBlocksOfType<IMyBeacon>(required: true).First();
            Output.Write($"Found beacon {Beacon.CustomName}");
            yield return Next();

            Battery = this.GetBlocksOfType<IMyBatteryBlock>(required: true).First();
            Output.Write($"Found battery {Battery.CustomName}");
            yield return Next();

            Guns = this.GetBlocksOfType<IMyLargeGatlingTurret>(required: true);
            Output.Write($"Found {Guns.Count} turrets");
            yield return Next();

            Lights = this.GetBlocksOfType<IMyInteriorLight>();
            Output.Write($"Found {Lights.Count} lights");
            yield return Next();

            Inventories = this.GetInventories();
            Output.Write($"Found {Inventories.Count} inventories");
        }

        private IEnumerator<UpdateFrequency> UpdateBatteryStatus()
        {
            Output.WriteTitle("Update Status");
            var batteryCharge = Math.Round(Battery.CurrentStoredPower * 100 / Battery.MaxStoredPower, 0);
            Output.Write($"Battery Level: {batteryCharge}%");
            yield return Next();

            var ammoCount = this.GetInventoryItems(Inventories, "MyObjectBuilder_AmmoMagazine", "NATO_25x184mm").Sum(i => i.Amount.ToIntSafe());
            Output.Write($"Ammo: {ammoCount}");
            yield return Next();

            var isDamaged = Guns.Any(g => g.DisassembleRatio < 1);
            var isBroken = Guns.Any(g => !g.IsFunctional);
            Output.Write($"Damaged: {isDamaged}");
            Output.Write($"Broken: {isBroken}");
            yield return Next();

            string prefix = Beacon.HudText;
            int prefixEnd = prefix.IndexOf(" [");
            Beacon.HudText = string.Format("{0} [{1}, {2}%]", prefixEnd > -1 ? prefix.Substring(0, prefixEnd) : prefix,
                ammoCount, batteryCharge);

            if (isBroken || ammoCount < Guns.Count * 6)
            {
                Lights.ForEach(l =>
                {
                    l.BlinkIntervalSeconds = 0.5f;
                    l.BlinkLength = 50f;
                });
            }
            else if (isDamaged || ammoCount < Guns.Count * 12)
            {
                Lights.ForEach(l =>
                {
                    l.BlinkIntervalSeconds = 2;
                    l.BlinkLength = 50f;
                });
            }
            else
            {
                Lights.ForEach(l =>
                {
                    l.BlinkIntervalSeconds = 0;
                    l.BlinkLength = 0;
                });
            }
        }
    }
}
