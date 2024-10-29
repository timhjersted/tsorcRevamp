using System;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
	public class BossBagEdits : GlobalItem
	{
		public override void ModifyItemLoot(Item item, ItemLoot itemLoot) 
        {
            if (item.type == ItemID.KingSlimeBossBag)
            {
                itemLoot.RemoveWhere(_ => true);
                itemLoot.Add(ItemDropRule.Common(ItemID.Solidifier));
                itemLoot.Add(ItemDropRule.Common(ItemID.SlimySaddle));
                itemLoot.Add(ItemDropRule.Common(ItemID.NinjaHood));
                itemLoot.Add(ItemDropRule.Common(ItemID.NinjaShirt));
                itemLoot.Add(ItemDropRule.Common(ItemID.NinjaPants));
                itemLoot.Add(ItemDropRule.Common(ItemID.SlimeHook));
                itemLoot.Add(ItemDropRule.Common(ItemID.SlimeGun));
                itemLoot.Add(ItemDropRule.Common(ItemID.SlimeStaff));
                itemLoot.Add(ItemDropRule.Common(ItemID.RoyalGel));
                itemLoot.Add(ItemDropRule.Common(ItemID.KingSlimeMask, 7));
            }

            if (item.type == ItemID.QueenBeeBossBag)
            {
                itemLoot.RemoveWhere(_ => true);
                itemLoot.Add(ItemDropRule.Common(ItemID.BeeWax, 1, 28, 38));
                itemLoot.Add(ItemDropRule.Common(ItemID.BeeKeeper));
                itemLoot.Add(ItemDropRule.Common(ItemID.BeesKnees));
                itemLoot.Add(ItemDropRule.Common(ItemID.BeeGun));
                itemLoot.Add(ItemDropRule.Common(ItemID.Beenade, 1, 25, 40));
                itemLoot.Add(ItemDropRule.Common(ItemID.HoneyComb));
                itemLoot.Add(ItemDropRule.Common(ItemID.HiveWand));
                itemLoot.Add(ItemDropRule.Common(ItemID.BeeHat));
                itemLoot.Add(ItemDropRule.Common(ItemID.BeeShirt));
                itemLoot.Add(ItemDropRule.Common(ItemID.BeePants));
                itemLoot.Add(ItemDropRule.Common(ItemID.Nectar));
                itemLoot.Add(ItemDropRule.Common(ItemID.HoneyedGoggles));
                itemLoot.Add(ItemDropRule.Common(ItemID.BottledHoney, 1, 10, 25));
                itemLoot.Add(ItemDropRule.Common(ItemID.HiveBackpack));
                itemLoot.Add(ItemDropRule.Common(ItemID.BeeMask, 7));
            }

            if (item.type == ItemID.WallOfFleshBossBag)
            {
                itemLoot.RemoveWhere(_ => true);
                itemLoot.Add(ItemDropRule.Common(ItemID.BadgersHat));
                itemLoot.Add(ItemDropRule.Common(ItemID.DemonHeart));
                itemLoot.Add(ItemDropRule.Common(ItemID.Pwnhammer));
                itemLoot.Add(ItemDropRule.Common(ItemID.SorcererEmblem));
                itemLoot.Add(ItemDropRule.Common(ItemID.WarriorEmblem));
                itemLoot.Add(ItemDropRule.Common(ItemID.RangerEmblem));
                itemLoot.Add(ItemDropRule.Common(ItemID.SummonerEmblem));
                itemLoot.Add(ItemDropRule.Common(ItemID.BreakerBlade));
                itemLoot.Add(ItemDropRule.Common(ItemID.ClockworkAssaultRifle));
                itemLoot.Add(ItemDropRule.Common(ItemID.LaserRifle));
                itemLoot.Add(ItemDropRule.Common(ItemID.FireWhip));
                itemLoot.Add(ItemDropRule.Common(ItemID.FleshMask, 7));
            }

            if (item.type == ItemID.QueenSlimeBossBag)
            {
                itemLoot.RemoveWhere(_ => true);
                itemLoot.Add(ItemDropRule.Common(ItemID.GelBalloon, 1, 50, 100));
                itemLoot.Add(ItemDropRule.Common(ItemID.VolatileGelatin));
                itemLoot.Add(ItemDropRule.Common(ItemID.CrystalNinjaHelmet));
                itemLoot.Add(ItemDropRule.Common(ItemID.CrystalNinjaChestplate));
                itemLoot.Add(ItemDropRule.Common(ItemID.CrystalNinjaLeggings));
                itemLoot.Add(ItemDropRule.Common(ItemID.QueenSlimeMountSaddle));
                itemLoot.Add(ItemDropRule.Common(ItemID.QueenSlimeHook));
                itemLoot.Add(ItemDropRule.Common(ItemID.Smolstar));
                itemLoot.Add(ItemDropRule.Common(ItemID.QueenSlimeTrophy, 7));
            }

            
            if (item.type == ItemID.PlanteraBossBag)
            {
                itemLoot.RemoveWhere(_ => true);
                itemLoot.Add(ItemDropRule.Common(ItemID.TempleKey));
                itemLoot.Add(ItemDropRule.Common(ItemID.GrenadeLauncher));
                itemLoot.Add(ItemDropRule.Common(ItemID.RocketI, 1, 150, 200));
                itemLoot.Add(ItemDropRule.Common(ItemID.VenusMagnum));
                itemLoot.Add(ItemDropRule.Common(ItemID.NettleBurst));
                itemLoot.Add(ItemDropRule.Common(ItemID.LeafBlower));
                itemLoot.Add(ItemDropRule.Common(ItemID.FlowerPow));
                itemLoot.Add(ItemDropRule.Common(ItemID.WaspGun));
                itemLoot.Add(ItemDropRule.Common(ItemID.Seedler));
                itemLoot.Add(ItemDropRule.Common(ItemID.PygmyStaff));
                itemLoot.Add(ItemDropRule.Common(ItemID.ThornHook));
                itemLoot.Add(ItemDropRule.Common(ItemID.TheAxe));
                itemLoot.Add(ItemDropRule.Common(ItemID.Seedling));
                itemLoot.Add(ItemDropRule.Common(ItemID.PlanteraMask, 7));
            }

			if (item.type == ItemID.GolemBossBag)
            {
                itemLoot.RemoveWhere(_ => true);
                itemLoot.Add(ItemDropRule.Common(ItemID.Stynger, 1, 200, 300));
                itemLoot.Add(ItemDropRule.Common(ItemID.StyngerBolt));
                itemLoot.Add(ItemDropRule.Common(ItemID.PossessedHatchet));
                itemLoot.Add(ItemDropRule.Common(ItemID.SunStone));
                itemLoot.Add(ItemDropRule.Common(ItemID.EyeoftheGolem));
                itemLoot.Add(ItemDropRule.Common(ItemID.HeatRay));
                itemLoot.Add(ItemDropRule.Common(ItemID.StaffofEarth));
                itemLoot.Add(ItemDropRule.Common(ItemID.GolemFist));
                itemLoot.Add(ItemDropRule.Common(ItemID.ShinyStone));
                itemLoot.Add(ItemDropRule.Common(ItemID.BeetleHusk, 1, 40, 50));
                itemLoot.Add(ItemDropRule.Common(ItemID.GolemMask, 7));
            }

            if (item.type == ItemID.FairyQueenBossBag)
            {
                itemLoot.RemoveWhere(_ => true);
                itemLoot.Add(ItemDropRule.Common(ItemID.FairyQueenMagicItem));
                itemLoot.Add(ItemDropRule.Common(ItemID.FairyQueenRangedItem));
                itemLoot.Add(ItemDropRule.Common(ItemID.PiercingStarlight));
                itemLoot.Add(ItemDropRule.Common(ItemID.RainbowWhip));
                itemLoot.Add(ItemDropRule.Common(ItemID.RainbowWings));
                itemLoot.Add(ItemDropRule.Common(ItemID.HallowBossDye, 1, 3, 3));
                itemLoot.Add(ItemDropRule.Common(ItemID.RainbowCursor));
                itemLoot.Add(ItemDropRule.Common(ItemID.EmpressFlightBooster));
                itemLoot.Add(ItemDropRule.Common(ItemID.FairyQueenMask, 7));
            }

            if (item.type == ItemID.FishronBossBag)
            {
                itemLoot.RemoveWhere(_ => true);
                itemLoot.Add(ItemDropRule.Common(ItemID.TempestStaff));
                itemLoot.Add(ItemDropRule.Common(ItemID.RazorbladeTyphoon));
                itemLoot.Add(ItemDropRule.Common(ItemID.BubbleGun));
                itemLoot.Add(ItemDropRule.Common(ItemID.Tsunami));
                itemLoot.Add(ItemDropRule.Common(ItemID.FishronWings));
                itemLoot.Add(ItemDropRule.Common(ItemID.ShrimpyTruffle));
                itemLoot.Add(ItemDropRule.Common(ItemID.DukeFishronMask, 7));
            }

            if (item.type == ItemID.BossBagBetsy)
            {
                itemLoot.RemoveWhere(_ => true);
                itemLoot.Add(ItemDropRule.Common(ItemID.BetsyWings));
                itemLoot.Add(ItemDropRule.Common(ItemID.MonkStaffT3));
                itemLoot.Add(ItemDropRule.Common(ItemID.DD2BetsyBow));
                itemLoot.Add(ItemDropRule.Common(ItemID.ApprenticeStaffT3));
                itemLoot.Add(ItemDropRule.Common(ItemID.DD2SquireBetsySword));
                itemLoot.Add(ItemDropRule.Common(ItemID.DefenderMedal, 1, 60, 100));
                itemLoot.Add(ItemDropRule.Common(ItemID.BossMaskBetsy, 7));
            }

            if (item.type == ItemID.MoonLordBossBag)
            {
                itemLoot.RemoveWhere(_ => true);
                itemLoot.Add(ItemDropRule.Common(ItemID.GravityGlobe));
                itemLoot.Add(ItemDropRule.Common(ItemID.SuspiciousLookingTentacle));
                itemLoot.Add(ItemDropRule.Common(ItemID.LongRainbowTrailWings));
                itemLoot.Add(ItemDropRule.Common(ItemID.Meowmere));
                itemLoot.Add(ItemDropRule.Common(ItemID.StarWrath));
                itemLoot.Add(ItemDropRule.Common(ItemID.Terrarian));
                itemLoot.Add(ItemDropRule.Common(ItemID.SDMG));
                itemLoot.Add(ItemDropRule.Common(ItemID.Celeb2));
                itemLoot.Add(ItemDropRule.Common(ItemID.LastPrism));
                itemLoot.Add(ItemDropRule.Common(ItemID.LunarFlareBook));
                itemLoot.Add(ItemDropRule.Common(ItemID.MoonlordTurretStaff));
                itemLoot.Add(ItemDropRule.Common(ItemID.RainbowCrystalStaff));
                itemLoot.Add(ItemDropRule.Common(ItemID.PortalGun));
                itemLoot.Add(ItemDropRule.Common(ItemID.LunarOre, 1, 140, 180));
                itemLoot.Add(ItemDropRule.Common(ItemID.MeowmereMinecart));
                itemLoot.Add(ItemDropRule.Common(ItemID.BossMaskMoonlord, 7));
            }
        }
    }
}