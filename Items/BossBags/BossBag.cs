using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.Items.Pets;

namespace tsorcRevamp.Items.BossBags {
	
	public abstract class BossBag : ModItem {
		
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Treasure Bag");
			Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
			
		}

		public override void SetDefaults() {
			item.maxStack = 999;
			item.consumable = true;
			item.width = 24;
			item.height = 24;
			item.rare = ItemRarityID.Cyan;
			item.expert = true;
		}

		public override bool CanRightClick() {
			return true;
		}
		
    }

    #region PreHardMode
    public class SlograBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Slogra>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(BossBagNPC))
            {
                if (Slain[BossBagNPC] == 0)
                {
                    //Slogra and Gaibon being a pair means it has to work a little different
                    //In normal mode, each of them drops half the souls. This ensures the full amount drops in expert mode instead.
                    player.QuickSpawnItem(ModContent.ItemType<DarkSoul>(), (int)((7000) * tsorcRevampPlayer.CheckSoulsMultiplier(player)));
                    Slain[BossBagNPC] = 1;
                    Slain[ModContent.NPCType<Gaibon>()] = 1;
                }
            }
            if (Main.rand.Next(9) == 0) player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.PoisonbiteRing>(), 1);
            if (Main.rand.Next(9) == 0) player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.BloodbiteRing>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<DarkSoul>(), (int)((700 + Main.rand.Next(300)) * tsorcRevampPlayer.CheckSoulsMultiplier(player)));
        }
    }
    public class JungleWyvernBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(BossBagNPC))
            {
                if (Slain[BossBagNPC] == 0)
                {
                    VanillaBossBag.AddBossBagSouls(BossBagNPC, player);
                    Slain[BossBagNPC] = 1;
                }
            }
            player.QuickSpawnItem(ItemID.Sapphire, Main.rand.Next(2, 10));
            player.QuickSpawnItem(ItemID.Ruby, Main.rand.Next(2, 10));
            player.QuickSpawnItem(ItemID.Topaz, Main.rand.Next(2, 10));
            player.QuickSpawnItem(ItemID.Diamond, Main.rand.Next(2, 10));
            player.QuickSpawnItem(ItemID.Emerald, Main.rand.Next(2, 10));
            player.QuickSpawnItem(ItemID.Amethyst, Main.rand.Next(2, 10));
            player.QuickSpawnItem(ItemID.Amethyst, Main.rand.Next(2, 10));
            player.QuickSpawnItem(ItemID.NecroHelmet);
            player.QuickSpawnItem(ItemID.NecroBreastplate);
            player.QuickSpawnItem(ItemID.NecroGreaves);
        }
    }
    #endregion

    #region Hardmode
    public class TheHunterBag : BossBag {
		public override int BossBagNPC => ModContent.NPCType<TheHunter>();
        public override void OpenBossBag(Player player) {
			var Slain = tsorcRevampWorld.Slain;
			if (Slain.ContainsKey(ModContent.NPCType<TheHunter>())) { //if the boss has been killed
				if (Slain[ModContent.NPCType<TheHunter>()] == 0) { //and the key value is 0
					VanillaBossBag.AddBossBagSouls(ModContent.NPCType<TheHunter>(), player); //give the player souls
					Slain[ModContent.NPCType<TheHunter>()] = 1; //set the value to 1aa
				}
			}
			player.QuickSpawnItem(ModContent.ItemType<Accessories.WaterShoes>());
            player.QuickSpawnItem(ModContent.ItemType<CrestOfEarth>(), 1);
			player.QuickSpawnItem(ItemID.Drax);
		}
    }
	public class TheRageBag : BossBag {
		public override int BossBagNPC => ModContent.NPCType<TheRage>();
		public override void OpenBossBag(Player player) {
			var Slain = tsorcRevampWorld.Slain;
			if (Slain.ContainsKey(ModContent.NPCType<TheRage>())) {
				if (Slain[ModContent.NPCType<TheRage>()] == 0) {
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<TheRage>(), player);
					Slain[ModContent.NPCType<TheRage>()] = 1;
				}
			}
			player.QuickSpawnItem(ModContent.ItemType<CrestOfFire>(), 1);
			player.QuickSpawnItem(ItemID.CobaltDrill);
		}
    }
    public class TheSorrowBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<TheSorrow>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<TheSorrow>()))
            {
                if (Slain[ModContent.NPCType<TheSorrow>()] == 0)
                {
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<TheSorrow>(), player);
                    Slain[ModContent.NPCType<TheSorrow>()] = 1;
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<CrestOfWater>());
            player.QuickSpawnItem(ItemID.AdamantiteDrill);
        }
    }

    public class AttraidiesBag : BossBag {
		public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>();
		public override void OpenBossBag(Player player) {
			var Slain = tsorcRevampWorld.Slain;
			if (Slain.ContainsKey(BossBagNPC)) {
                if (Slain[BossBagNPC] == 0) {
                    VanillaBossBag.AddBossBagSouls(BossBagNPC, player);
					Slain[BossBagNPC] = 1;
				}
			}
            player.QuickSpawnItem(ModContent.ItemType<Items.TheEnd>());
            player.QuickSpawnItem(ModContent.ItemType<Items.GuardianSoul>());
            player.QuickSpawnItem(ModContent.ItemType<Items.SoulOfAttraidies>(), Main.rand.Next(15, 23));
            player.QuickSpawnItem(ModContent.ItemType<Items.DarkSoul>(), 2000);
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Magic.BloomShards>());
            player.QuickSpawnItem(ItemID.Picksaw);
        }
	}
    public class KrakenBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Fiends.WaterFiendKraken>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(BossBagNPC))
            {
                if (Slain[BossBagNPC] == 0)
                {
                    VanillaBossBag.AddBossBagSouls(BossBagNPC, player);
                    Slain[BossBagNPC] = 1;
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.DragonHorn>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.GuardianSoul>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Melee.ForgottenRisingSun>(), 10);
        }
    }
    public class MarilithBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(BossBagNPC))
            {
                if (Slain[BossBagNPC] == 0)
                {
                    VanillaBossBag.AddBossBagSouls(BossBagNPC, player);
                    Slain[BossBagNPC] = 1;
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.Potions.HolyWarElixir>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.GuardianSoul>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.FairyInABottle>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Magic.Ice3Tome>(), 1);
        }
    }
    public class LichBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Fiends.EarthFiendLich>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(BossBagNPC))
            {
                if (Slain[BossBagNPC] == 0)
                {
                    VanillaBossBag.AddBossBagSouls(BossBagNPC, player);
                    Slain[BossBagNPC] = 1;
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.Potions.HolyWarElixir>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.GuardianSoul>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.FairyInABottle>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Magic.Bolt3Tome>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.DragoonBoots>(), 1);
        }
    }
    
    public class SerrisBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(BossBagNPC))
            {
                if (Slain[BossBagNPC] == 0)
                {
                    player.QuickSpawnItem(ModContent.ItemType<Items.DarkSoul>(), (int)(50000 * tsorcRevampPlayer.CheckSoulsMultiplier(player)));
                    //VanillaBossBag.AddBossBagSouls(BossBagNPC, player);
                    Slain[BossBagNPC] = 1;
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.Potions.DemonDrugPotion>(), 3 + Main.rand.Next(4));
            player.QuickSpawnItem(ModContent.ItemType<Items.Potions.ArmorDrugPotion>(), 3 + Main.rand.Next(4));
            player.QuickSpawnItem(ModContent.ItemType<Items.GuardianSoul>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Magic.BarrierTome>(), 1);
        }
    }
    public class DeathBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Death>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(BossBagNPC))
            {
                if (Slain[BossBagNPC] == 0)
                {
                    VanillaBossBag.AddBossBagSouls(BossBagNPC, player);
                    Slain[BossBagNPC] = 1;
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.Potions.HolyWarElixir>(), 4);
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Magic.WallTome>(), 4);
            player.QuickSpawnItem(ModContent.ItemType<Items.GuardianSoul>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Magic.BarrierTome>(), 1);
            player.QuickSpawnItem(ItemID.MidnightRainbowDye, 5);
        }
    }
    public class WyvernMageBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(BossBagNPC))
            {
                if (Slain[BossBagNPC] == 0)
                {
                    VanillaBossBag.AddBossBagSouls(BossBagNPC, player);
                    Slain[BossBagNPC] = 1;
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.Potions.HolyWarElixir>(), 2);
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Melee.LionheartGunblade>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Magic.LampTome>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.GemBox>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.PoisonbiteRing>(), 1);
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.BloodbiteRing>(), 1);
        }
    }
    #endregion

    #region SuperHardMode
    public class GwynBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<TheHunter>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<Gwyn>()))
            { //if the boss has been killed
                if (Slain[ModContent.NPCType<Gwyn>()] == 0)
                { //and the key value is 0
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<Gwyn>(), player); //give the player souls
                    Slain[ModContent.NPCType<Gwyn>()] = 1; //set the value to 1
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.GuardianSoul>());
            player.QuickSpawnItem(ModContent.ItemType<Items.DraxEX>());
            player.QuickSpawnItem(ModContent.ItemType<Items.Epilogue>());
            player.QuickSpawnItem(ModContent.ItemType<Items.EssenceOfTerraria>());
        }
    }
    public class BlightBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<Blight>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<Blight>()))
            { //if the boss has been killed
                if (Slain[ModContent.NPCType<Blight>()] == 0)
                { //and the key value is 0
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<Blight>(), player); //give the player souls
                    Slain[ModContent.NPCType<Blight>()] = 1; //set the value to 1
                }
            }

            player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Magic.DivineSpark>());
            player.QuickSpawnItem(ModContent.ItemType<Items.SoulOfBlight>(), Main.rand.Next(3, 6));
        }
    }
    public class ChaosBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<Chaos>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<Chaos>()))
            { //if the boss has been killed
                if (Slain[ModContent.NPCType<Chaos>()] == 0)
                { //and the key value is 0
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<Chaos>(), player); //give the player souls
                    Slain[ModContent.NPCType<Chaos>()] = 1; //set the value to 1
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.Armors.PowerArmorNUHelmet>());
            player.QuickSpawnItem(ModContent.ItemType<Items.Armors.PowerArmorNUTorso>());
            player.QuickSpawnItem(ModContent.ItemType<Items.Armors.PowerArmorNUGreaves>());
            if (Main.rand.Next(99) < 20) player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Magic.FlareTome>());
            if (Main.rand.Next(99) < 40) player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Ranged.ElfinBow>());
            player.QuickSpawnItem(ModContent.ItemType<Items.Potions.HolyWarElixir>());
            if (Main.rand.Next(99) < 50) player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Melee.HiRyuuSpear>());
            player.QuickSpawnItem(ModContent.ItemType<Items.DarkSoul>(), 3000);
            player.QuickSpawnItem(ModContent.ItemType<Items.GuardianSoul>());
            player.QuickSpawnItem(ModContent.ItemType<Items.SoulOfChaos>(), 3);
        }
    }
    public class MageShadowBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()))
            { //if the boss has been killed
                if (Slain[ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()] == 0)
                { //and the key value is 0
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>(), player); //give the player souls
                    Slain[ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()] = 1; //set the value to 1
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.Potions.HolyWarElixir>(), 4);
            player.QuickSpawnItem(ModContent.ItemType<Items.GhostWyvernSoul>(), 8);
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.RingOfPower>());
        }
    }
    public class GhostWyvernBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()))
            { //if the boss has been killed
                if (Slain[ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()] == 0)
                { //and the key value is 0
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>(), player); //give the player souls
                    Slain[ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()] = 1; //set the value to 1
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.Potions.HolyWarElixir>(), 4);
            player.QuickSpawnItem(ModContent.ItemType<Items.GhostWyvernSoul>(), 8);
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.RingOfPower>());
        }
    }

    public class OolacileSorcererBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<AbysmalOolacileSorcerer>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<AbysmalOolacileSorcerer>()))
            { //if the boss has been killed
                if (Slain[ModContent.NPCType<AbysmalOolacileSorcerer>()] == 0)
                { //and the key value is 0
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<AbysmalOolacileSorcerer>(), player); //give the player souls
                    Slain[ModContent.NPCType<AbysmalOolacileSorcerer>()] = 1; //set the value to 1
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.Potions.HealingElixir>(), 10);
            player.QuickSpawnItem(ModContent.ItemType<Items.DarkSoul>(), 5000);
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.DuskCrownRing>());
            player.QuickSpawnItem(ModContent.ItemType<Items.Humanity>());
            if (Main.rand.Next(1) == 0) player.QuickSpawnItem(ModContent.ItemType<Items.PurgingStone>());
            player.QuickSpawnItem(ModContent.ItemType<Items.RedTitanite>(), 5);
        }
    }
    public class ArtoriasBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<Artorias>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<Artorias>()))
            { //if the boss has been killed
                if (Slain[ModContent.NPCType<Artorias>()] == 0)
                { //and the key value is 0
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<Artorias>(), player); //give the player souls
                    Slain[ModContent.NPCType<Artorias>()] = 1; //set the value to 1
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.GuardianSoul>());
            player.QuickSpawnItem(ModContent.ItemType<Items.DarkSoul>(), 5000);
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.WolfRing>());
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.TheRingOfArtorias>());
            player.QuickSpawnItem(ModContent.ItemType<Items.SoulOfArtorias>(), 4);
        }
    }
    public class DarkCloudBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<DarkCloud>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<DarkCloud>()))
            { //if the boss has been killed
                if (Slain[ModContent.NPCType<DarkCloud>()] == 0)
                { //and the key value is 0
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<DarkCloud>(), player); //give the player souls
                    Slain[ModContent.NPCType<DarkCloud>()] = 1; //set the value to 1
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.GuardianSoul>());
            player.QuickSpawnItem(ModContent.ItemType<Items.Humanity>(), 3);
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.DuskCrownRing>());
        }
    }
    public class HellkiteBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()))
            { //if the boss has been killed
                if (Slain[ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()] == 0)
                { //and the key value is 0
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>(), player); //give the player souls
                    Slain[ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()] = 1; //set the value to 1
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.DragonScale>(), 22 + Main.rand.Next(6));
            player.QuickSpawnItem(ModContent.ItemType<Items.DarkSoul>(), 4000);
            if (Main.rand.Next(99) < 10) player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.DragonStone>());
        }
    }
    public class SeathBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>()))
            { //if the boss has been killed
                if (Slain[ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>()] == 0)
                { //and the key value is 0
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>(), player); //give the player souls
                    Slain[ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>()] = 1; //set the value to 1
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<Items.DarkSoul>(), 7000);
            player.QuickSpawnItem(ModContent.ItemType<Items.BequeathedSoul>(), 2);
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.BlueTearstoneRing>());
            player.QuickSpawnItem(ModContent.ItemType<Items.PurgingStone>());
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.DragonWings>());
        }
    }
    public class WitchkingBag : BossBag
    {
        public override int BossBagNPC => ModContent.NPCType<TheHunter>();
        public override void OpenBossBag(Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(ModContent.NPCType<Witchking>()))
            { //if the boss has been killed
                if (Slain[ModContent.NPCType<Witchking>()] == 0)
                { //and the key value is 0
                    VanillaBossBag.AddBossBagSouls(ModContent.NPCType<Witchking>(), player); //give the player souls
                    Slain[ModContent.NPCType<Witchking>()] = 1; //set the value to 1
                }
            }
            player.QuickSpawnItem(ModContent.ItemType<BrokenStrangeMagicRing>());
            if (Main.rand.NextFloat() <= .12f) player.QuickSpawnItem(ModContent.ItemType<Items.Weapons.Melee.WitchkingsSword>());
            if (Main.rand.Next(10) == 0) player.QuickSpawnItem(ModContent.ItemType<Items.Armors.WitchkingHelmet>());
            if (Main.rand.Next(10) == 0) player.QuickSpawnItem(ModContent.ItemType<Items.Armors.WitchkingTop>());
            if (Main.rand.Next(10) == 0) player.QuickSpawnItem(ModContent.ItemType<Items.Armors.WitchkingBottoms>());
            if (Main.rand.Next(20) == 0) player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.RingOfPower>());
            if (Main.rand.NextFloat() <= .08f) player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.GoldenHairpin>());
            if (Main.rand.NextFloat() <= .15f) player.QuickSpawnItem(ModContent.ItemType<GuardianSoul>());
            if (Main.rand.Next(2) == 0) player.QuickSpawnItem(ModContent.ItemType<Items.BossItems.DarkMirror>());
            player.QuickSpawnItem(ModContent.ItemType<Items.Accessories.CovenantOfArtorias>());
            player.QuickSpawnItem(ModContent.ItemType<DarkSoul>(), 2500);
        }
    }
    #endregion

    public class VanillaBossBag : GlobalItem {
        public static void AddBossBagSouls(int EnemyID, Player player) {
            NPC npc = new NPC();
            npc.SetDefaults(EnemyID);
            float enemyValue = (int)npc.value / 25;
            float multiplier = tsorcRevampPlayer.CheckSoulsMultiplier(player);
            tsorcRevampWorld.Slain[EnemyID] = 1; //set the value to 1

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData); //Slain only exists on the server. This tells the server to run NetSend(), which syncs this data with clients
            }

            int DarkSoulQuantity = (int)(multiplier * enemyValue);

            player.QuickSpawnItem(ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
        }

        public static void SoulsOnFirstBag(int EnemyID, Player player) {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(EnemyID)) {
                if (Slain[EnemyID] == 0) {
                    AddBossBagSouls(EnemyID, player);
                    Slain[EnemyID] = 1;
                }
            }
        }

        public override bool PreOpenVanillaBag(string context, Player player, int arg) {
            
            if (context == "bossBag" && arg == ItemID.KingSlimeBossBag) { //re-implement king slime bag to stop blacklisted items from dropping in adventure mode
                player.QuickSpawnItem(ItemID.RoyalGel);
                player.QuickSpawnItem(ItemID.Solidifier);
                player.QuickSpawnItem(ItemID.GoldCoin, 11);
                player.QuickSpawnItem(ItemID.Katana);
                if (Main.rand.Next(99) < 66) { player.QuickSpawnItem(ItemID.NinjaHood); }
                if (Main.rand.Next(99) < 66) { player.QuickSpawnItem(ItemID.NinjaShirt); }
                if (Main.rand.Next(99) < 66) { player.QuickSpawnItem(ItemID.NinjaPants); }
                if (Main.rand.Next(7) == 0) { player.QuickSpawnItem(ItemID.KingSlimeMask); }
                if (Main.rand.Next(10) == 0) { player.QuickSpawnItem(ItemID.KingSlimeTrophy); }
                if (Main.rand.Next(2) == 0) { player.QuickSpawnItem(ItemID.SlimeGun); }
                if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) { //no hooks or saddles in adventure mode
                    if (Main.rand.Next(2) == 0) { player.QuickSpawnItem(ItemID.SlimeHook); }
                    if (Main.rand.Next(2) == 0) { player.QuickSpawnItem(ItemID.SlimySaddle); }
                }
                SoulsOnFirstBag(NPCID.KingSlime, player);
                return false;
            }
            if (context == "bossBag" && arg == ItemID.GolemBossBag)
            { 
                //Picksaw drops from Attraidies who is Post-Golem now, and gates SuperHardMode content. We've gotta stop Golem from dropping it.
                if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                {
                    if (Main.rand.Next(3) == 0) { player.QuickSpawnItem(ItemID.Picksaw); }
                }

                //Drops that work in the traditional way. Also, adds the Crest of Stone to its drops.
                player.QuickSpawnItem(ModContent.ItemType<CrestOfStone>());
                player.QuickSpawnItem(ItemID.ShinyStone);
                player.QuickSpawnItem(ModContent.ItemType<Items.BrokenPicksaw>());
                if (Main.rand.Next(6) == 0) { player.QuickSpawnItem(ItemID.GolemMask); }
                if (Main.rand.Next(9) == 0) { player.QuickSpawnItem(ItemID.GolemTrophy); }
                player.QuickSpawnItem(ItemID.GreaterHealingPotion, 5 + Main.rand.Next(10));

                //Always drops one of these things, picked at random
                int drop = Main.rand.Next(6);
                switch (drop) { 
                    case 0:
                        player.QuickSpawnItem(ItemID.Stynger);
                        player.QuickSpawnItem(ItemID.StyngerBolt, 60 + Main.rand.Next(39));
                        break;
                    case 1:
                        player.QuickSpawnItem(ItemID.PossessedHatchet);
                        break;
                    case 2:
                        player.QuickSpawnItem(ItemID.SunStone);
                        break;
                    case 3:
                        player.QuickSpawnItem(ItemID.EyeoftheGolem);
                        break;
                    case 4:
                        player.QuickSpawnItem(ItemID.HeatRay);
                        break;
                    case 5:
                        player.QuickSpawnItem(ItemID.StaffofEarth);
                        break;
                    case 6:
                        player.QuickSpawnItem(ItemID.GolemFist);
                        break;
                }

                SoulsOnFirstBag(NPCID.Golem, player);
                return false;
            }



            return base.PreOpenVanillaBag(context, player, arg);
        }
        public override void OpenVanillaBag(string context, Player player, int arg) {
            var Slain = tsorcRevampWorld.Slain;
            if (context == "bossBag") {
                if (arg == ItemID.EyeOfCthulhuBossBag) {
                    player.QuickSpawnItem(ItemID.HermesBoots);
                    player.QuickSpawnItem(ItemID.HerosHat);
                    player.QuickSpawnItem(ItemID.HerosPants);
                    player.QuickSpawnItem(ItemID.HerosShirt);
                    SoulsOnFirstBag(NPCID.EyeofCthulhu, player);
                }
                if (arg == ItemID.EaterOfWorldsBossBag) {
                    SoulsOnFirstBag(NPCID.EyeofCthulhu, player);
                }
                if (arg == ItemID.BrainOfCthulhuBossBag) {
                    SoulsOnFirstBag(NPCID.BrainofCthulhu, player);
                }
                if (arg == ItemID.QueenBeeBossBag) {
                    SoulsOnFirstBag(NPCID.QueenBee, player);
                }
                if (arg == ItemID.WallOfFleshBossBag) {
                    SoulsOnFirstBag(NPCID.WallofFlesh, player);
                }
                if (arg == ItemID.SkeletronBossBag) {
                    SoulsOnFirstBag(NPCID.SkeletronHead, player);
                    if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) player.QuickSpawnItem(ModContent.ItemType<Miakoda>());
                    if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) player.QuickSpawnItem(ModContent.ItemType<MiakodaFull>());
                }
                if (arg == ItemID.DestroyerBossBag) {
                    SoulsOnFirstBag(NPCID.TheDestroyer, player);
                    player.QuickSpawnItem(ModContent.ItemType<RTQ2>());
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfCorruption>(), 1);
                }
                if (arg == ItemID.TwinsBossBag) {
                    /* 
                    * picture the following:
                    * Twins are killed. Spazmatism is added to Slain, and the player opens a bag and receives souls
                    * then, Twins are killed again. Retinazer is added to slain this time, and the player opens a bag and gets souls again
                    * to prevent this, we need to make sure we haven't opened a bag from Spazmatism when we open a bag in Retinazer's context
                    */
                    if (Slain.ContainsKey(NPCID.Retinazer)) {
                        if (Slain[NPCID.Retinazer] == 0) {
                            bool SpazmatismDowned = Slain.TryGetValue(NPCID.Spazmatism, out int value);
                            //if SpazmatismDowned evaluates to true, int value is set to the value pair of Spazmatism's key, which stores if a bag has been opened
                            if (!SpazmatismDowned || value == 0) { //if Spazmatism is not in Slain, or no twins bag has been opened in Spazmatism's context
                                AddBossBagSouls(NPCID.Retinazer, player);
                                Slain[NPCID.Retinazer] = 1;
                            }
                        }
                    }
                    else if (Slain.ContainsKey(NPCID.Spazmatism)) { //dont need to check if Retinazer is downed, since this is only run if Retinazer is not in Slain
                        if (Slain[NPCID.Spazmatism] == 0) {
                            AddBossBagSouls(NPCID.Spazmatism, player);
                            Slain[NPCID.Spazmatism] = 1;
                        }
                    }
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfSky>(), 1);
                }
                if (arg == ItemID.SkeletronPrimeBossBag) {
                    SoulsOnFirstBag(NPCID.SkeletronPrime, player);
                    player.QuickSpawnItem(ItemID.AngelWings);
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfSteel>(), 1);
                }
                if (arg == ItemID.PlanteraBossBag)
                {
                    player.QuickSpawnItem(ModContent.ItemType<CrestOfLife>());
                    SoulsOnFirstBag(NPCID.Plantera, player);
                }
                if (arg == ItemID.FishronBossBag) {
                    SoulsOnFirstBag(NPCID.DukeFishron, player);
                }
                if (arg == ItemID.MoonLordBossBag) {
                    if (Slain.ContainsKey(NPCID.MoonLordCore)) {
                        if (Slain[NPCID.MoonLordCore] == 0) {
                            int enemyValue = 40000; // 1 platinum / 25
                            float multiplier = tsorcRevampPlayer.CheckSoulsMultiplier(player);

                            int DarkSoulQuantity = (int)(multiplier * enemyValue);

                            player.QuickSpawnItem(ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
                            Slain[NPCID.MoonLordCore] = 1;
                        }
                    }
                }
            }
        }
    }
}
