using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp;
using tsorcRevamp.Items.Pets;
using tsorcRevamp.NPCs.Bosses;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using Terraria.GameContent.ItemDropRules;
using System.Collections.Generic;

namespace tsorcRevamp.Items.BossBags
{

    public abstract class BossBag : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Treasure Bag");
            Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");

        }

        public override void SetDefaults()
        {
            Item.maxStack = 999;
            Item.consumable = true;
            Item.width = 24;
            Item.height = 24;
            Item.rare = ItemRarityID.Cyan;
            Item.expert = true;
        }

        public override bool CanRightClick()
        {
            return true;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            for (int i = 0; i < 4; i++)
            {
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0);
            }

            return true;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            //UsefulFunctions.DustRing(Item.Center, 32, DustID.ShadowbeamStaff);
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];

            Lighting.AddLight(Item.Center, Main.DiscoColor.ToVector3());
            for (int i = 0; i < 4; i++)
            {
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(((Main.GameUpdateCount % 300) / 30f) + MathHelper.PiOver2 * i) * 5;
                spriteBatch.Draw(texture,offsetPositon + new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Main.DiscoColor, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }

            return true;
        }

    }

    #region PreHardMode

    public class OolacileDemonBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.DragonCrestShield>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.BandOfCosmicPower>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.PermanentPotions.PermanentShinePotion>()));
            itemLoot.Add(ItemDropRule.Common(ItemID.CloudinaBottle));
        }
        
        public override int BossBagNPC => ModContent.NPCType<AncientOolacileDemon>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, false, true); //gives the player souls if they haven't opened the bag before
        }
    }

    public class SlograBag : BossBag
    {

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.BurningStone>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.PoisonbiteRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.BloodbiteRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Ranged.DarkTrident>()));
        }
        public override int BossBagNPC => ModContent.NPCType<Slogra>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            tsorcRevampWorld.Slain[ModContent.NPCType<Gaibon>()] = 1;
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, false, true); //gives the player souls if they haven't opened the bag before
            player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<DarkSoul>(), (int)((700 + Main.rand.Next(300)) * tsorcRevampPlayer.CheckSoulsMultiplier(player)));
        }
    }
    public class GaibonBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.BurningAura>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.PoisonbiteRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.BloodbiteRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Ranged.DarkTrident>()));
        }
        public override int BossBagNPC => ModContent.NPCType<Gaibon>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            tsorcRevampWorld.Slain[ModContent.NPCType<Slogra>()] = 1;
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, false, true); //gives the player souls if they haven't opened the bag before
            player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<DarkSoul>(), (int)((700 + Main.rand.Next(300)) * tsorcRevampPlayer.CheckSoulsMultiplier(player)));
        }
    }
    public class JungleWyvernBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.ChloranthyRing>()));

            itemLoot.Add(ItemDropRule.Common(ItemID.Amethyst, 1, 2, 10));
            itemLoot.Add(ItemDropRule.Common(ItemID.Topaz, 1, 2, 10));
            itemLoot.Add(ItemDropRule.Common(ItemID.Sapphire, 1, 2, 10));
            itemLoot.Add(ItemDropRule.Common(ItemID.Emerald, 1, 2, 10));
            itemLoot.Add(ItemDropRule.Common(ItemID.Ruby, 1, 2, 10));
            itemLoot.Add(ItemDropRule.Common(ItemID.Amber, 1, 2, 10));
            itemLoot.Add(ItemDropRule.Common(ItemID.Diamond, 1, 2, 10));
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, false, true); //gives the player souls if they haven't opened the bag before
        }
    }
    #endregion

    #region Hardmode

    public class AncientDemonBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Accessories.EyeOfTheGods>(), 1));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Defensive.BarrierRing>(), 1));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Expert.CrackedDragonStone>(), 1));
        }

        public override int BossBagNPC => ModContent.NPCType<AncientDemon>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, false, true); //gives the player souls if they haven't opened the bag before
        }
    }
    public class LumeliaBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.CovetousSilverSerpentRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulShekel>(), 1, 10, 20));
            itemLoot.Add(ItemDropRule.Common(ItemID.WaterWalkingBoots));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Ammo.ArrowOfBard>(), 1, 10, 20));
        }
        public override int BossBagNPC => ModContent.NPCType<HeroofLumelia>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, false, true); //gives the player souls if they haven't opened the bag before
        }
    }
    public class TheRageBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Summon.PhoenixEgg>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrestOfFire>()));
            itemLoot.Add(ItemDropRule.Common(ItemID.CobaltDrill));
        }
        public override int BossBagNPC => ModContent.NPCType<TheRage>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player); //gives the player souls if they haven't opened the bag before
        }
    }
    public class TheSorrowBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.GoldenHairpin>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrestOfWater>()));
            itemLoot.Add(ItemDropRule.Common(ItemID.AdamantiteDrill));
        }
        public override int BossBagNPC => ModContent.NPCType<TheSorrow>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player); //gives the player souls if they haven't opened the bag before
        }
    }
    public class TheHunterBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert item

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrestOfEarth>()));
            itemLoot.Add(ItemDropRule.Common(ItemID.Drax));
            itemLoot.Add(ItemDropRule.Common(ItemID.WaterWalkingBoots));
        }
        public override int BossBagNPC => ModContent.NPCType<TheHunter>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player); //gives the player souls if they haven't opened the bag before
        }
    }
    public class WyvernMageBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Broadswords.LionheartGunblade>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Magic.GemBox>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Magic.LampTome>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.HolyWarElixir>(), 1, 2, 2));
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, false, true); //gives the player souls if they haven't opened the bag before          
        }
    }
    public class SerrisBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert item

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.DemonDrugPotion>(), 1, 3, 7));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.ArmorDrugPotion>(), 1, 3, 7));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Magic.MagicBarrierScroll>()));
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, true, true); //gives the player souls if they haven't opened the bag before     
        }
    }
    public class DeathBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert item

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Magic.GreatMagicShieldScroll>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Magic.MagicBarrierScroll>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.HolyWarElixir>(), 1, 4, 4));
            itemLoot.Add(ItemDropRule.Common(ItemID.LivingRainbowDye, 1, 5, 5));
            itemLoot.Add(ItemDropRule.Common(ItemID.MidnightRainbowDye, 1, 5, 5));
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Death>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, true); //gives the player souls if they haven't opened the bag before            
        }
    }
    public class MindflayerIllusionBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //won't get an expert item, just a part of Attraidies

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BossItems.MindflayerIllusionRelic>()));
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Okiku.ThirdForm.BrokenOkiku>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player); //gives the player souls if they haven't opened the bag before
        }
    }
    public class AttraidiesBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Magic.BloomShards>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HeavenPiercer>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<TheEnd>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulOfAttraidies>(), 1, 15, 23));
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player); //gives the player souls if they haven't opened the bag before            
        }
    }
    #endregion

    #region SuperHardMode
    public class KrakenBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.DragoonHorn>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Shortswords.BarrowBlade>()));
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Fiends.WaterFiendKraken>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, true, true); //gives the player souls if they haven't opened the bag before
        }
    }
    public class MarilithBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert item

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Shortswords.BarrowBlade>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.ForgottenRisingSun>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Magic.Ice3Tome>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<FairyInABottle>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.HolyWarElixir>()));

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                itemLoot.Add(ItemDropRule.Common(ItemID.LargeSapphire));
            }
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, true, true); //gives the player souls if they haven't opened the bag before
        }
    }
    public class LichBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.DragoonBoots>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Magic.Bolt3Tome>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Broadswords.ForgottenGaiaSword>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.HolyWarElixir>()));
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.Fiends.EarthFiendLich>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, true, true); //gives the player souls if they haven't opened the bag before
        }
    }
    public class BlightBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert item

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Magic.DivineSpark>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulOfBlight>(), 1, 3, 6));
        }
        public override int BossBagNPC => ModContent.NPCType<Blight>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, true); //gives the player souls if they haven't opened the bag before           
        }
    }
    public class ChaosBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert item

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Magic.FlareTome>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Ranged.ElfinBow>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.HolyWarElixir>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulOfChaos>(), 1, 3, 3));
        }
        public override int BossBagNPC => ModContent.NPCType<Chaos>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, true); //gives the player souls if they haven't opened the bag before              
        }
    }
    public class MageShadowBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.RingOfPower>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.HolyWarElixir>(), 1, 4, 4));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<GhostWyvernSoul>(), 1, 8, 8));
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player); //gives the player souls if they haven't opened the bag before             
        }
    }

    public class OolacileSorcererBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.DuskCrownRing>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.HealingElixir>(), 1, 10, 10));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PurgingStone>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 5, 5));
        }
        public override int BossBagNPC => ModContent.NPCType<AbysmalOolacileSorcerer>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, true); //gives the player souls if they haven't opened the bag before           
        }
    }
    public class ArtoriasBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.RingofArtorias>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.WolfRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulOfArtorias>(), 1, 6, 6));
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                itemLoot.Add(ItemDropRule.Common(ItemID.LargeAmethyst));
            }
        }
        public override int BossBagNPC => ModContent.NPCType<Artorias>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, true); //gives the player souls if they haven't opened the bag before
        }
    }

    public class HellkiteBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.DragonStone>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BossItems.HellkiteStone>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.HiRyuuSpear>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DragonEssence>(), 1, 22, 28));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Shortswords.BarrowBlade>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulShekel>(), 1, 5, 10));
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, true); //gives the player souls if they haven't opened the bag before            
        }
    }
    public class SeathBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.DragonWings>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.BlueTearstoneRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PurgingStone>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DragonEssence>(), 1, 35, 40));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BequeathedSoul>(), 1, 3, 3));
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player); //gives the player souls if they haven't opened the bag before
        }
    }
    public class WitchkingBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Broadswords.WitchkingsSword>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Armors.Summon.WitchkingHelmet>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Armors.Summon.WitchkingTop>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Armors.Summon.WitchkingBottoms>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.CovenantOfArtorias>()));
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BrokenStrangeMagicRing>()));
            }
        }
        public override int BossBagNPC => ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player, true); //gives the player souls if they haven't opened the bag before
        }
    }
    public class DarkCloudBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.ReflectionShift>()));

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Broadswords.MoonlightGreatsword>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Summon.NullSpriteStaff>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<GuardianSoul>(), 1, 5, 5));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 3, 3));
        }
        public override int BossBagNPC => ModContent.NPCType<DarkCloud>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player); //gives the player souls if they haven't opened the bag before

        }
    }
    public class GwynBag : BossBag
    {
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert item

            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Epilogue>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<EssenceOfTerraria>()));


        }
        public override int BossBagNPC => ModContent.NPCType<Gwyn>();
        [System.Obsolete]
        public override void OpenBossBag(Player player)
        {
            VanillaBossBag.AddBossBagSouls(BossBagNPC, player); //gives the player souls if they haven't opened the bag before
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) //threw an error when loading the mod as I tried to put it into modifyitemloot, wouldn't accept Main.player[Main.myplayer].GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse for the bool
            {
                player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<DraxEX>(), 1);
            }
        }
    }
    #endregion

    public class VanillaBossBag : GlobalItem
    {
        public static void AddBossBagSouls(int EnemyID, Player player, bool guardianSoul = false, bool staminaVessel = false)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (!modPlayer.bagsOpened.Contains(EnemyID))
            {
                modPlayer.bagsOpened.Add(EnemyID);
            }
            else
            {
                return;
            }

            NPC npc = new NPC();
            npc.SetDefaults(EnemyID);
            float enemyValue = (int)npc.value / 25f;
            float multiplier = tsorcRevampPlayer.CheckSoulsMultiplier(player);
            tsorcRevampWorld.Slain[EnemyID] = 1; //set the value to 1

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData); //Slain only exists on the server. This tells the server to run NetSend(), which syncs this data with clients
            }

            int DarkSoulQuantity = (int)(multiplier * enemyValue);

            if (guardianSoul)
            {
                player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<GuardianSoul>());
            }
            if (staminaVessel)
            {
                player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<StaminaVessel>());
            }

            player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
        }

        public static void GiveDarkSouls(int bossBagID, Player player) {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.bagsOpened.Contains(bossBagID)) {
                return;
            }
            NPC boss = new NPC();
            boss.SetDefaults(tsorcRevamp.BossBagIDtoNPCID[bossBagID]);
            float bossValue = boss.value / 25f;
            float multiplier = tsorcRevampPlayer.CheckSoulsMultiplier(player);
            player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<DarkSoul>(), (int)(multiplier * bossValue));
            modPlayer.bagsOpened.Add(bossBagID);
        }

        public override void RightClick(Item item, Player player) 
        {
            // check if an item is a Treasure Bag
            if (!tsorcRevamp.BossBagIDtoNPCID.ContainsKey(item.type)) {
                return;
            }

            GiveDarkSouls(item.type, player);
        }

        public override void ModifyItemLoot(Item item, ItemLoot loot) 
        {
            // check if an item is a Treasure Bag
            if (!tsorcRevamp.BossBagIDtoNPCID.ContainsKey(item.type)) {
                return;
            }

			int itemID = item.type;

            // take into account blocked items
            List<IItemDropRule> dropRules = loot.Get();
            foreach (var rule in dropRules) {
                List<int> ruleItems = new List<int>(){};
                if (rule is CommonDrop) {
                    ruleItems.Add(((CommonDrop)rule).itemId);
                } else if (rule is DropOneByOne) {
                    ruleItems.Add(((DropOneByOne)rule).itemId);
                } else if (rule is OneFromOptionsDropRule) {
                    foreach (var dropId in ((OneFromOptionsDropRule)rule).dropIds) {
                        ruleItems.Add(dropId);
                    }
                } else if (rule is OneFromOptionsNotScaledWithLuckDropRule) {
                    foreach (var dropId in ((OneFromOptionsNotScaledWithLuckDropRule)rule).dropIds) {
                        ruleItems.Add(dropId);
                    }
                } else {
                    continue;
                }

                foreach (var itemToRemove in tsorcRevamp.RemovedBossBagLoot[itemID]) {
                    if (ruleItems.Contains(itemToRemove)) {
                        loot.Remove(rule);
                        continue;
                    }
                }
            }

            // add needed extras to Treasure Bags
            tsorcRevamp.BossExtras assignedExtras = tsorcRevamp.AssignedBossExtras[itemID];
            foreach (var it in tsorcRevamp.BossExtrasDescription) {
                if ((assignedExtras & it.Key) != 0) {
                    loot.Add(ItemDropRule.ByCondition(it.Value.Condition, it.Value.ID));
                }
            }

            // add other extra items to Treasure Bags
            foreach (var dropRule in tsorcRevamp.AddedBossBagLoot[itemID]) {
                loot.Add(dropRule);
            }
		}

        public static void SoulsOnFirstBag(int EnemyID, Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(EnemyID))
            {
                if (Slain[EnemyID] == 0)
                {
                    AddBossBagSouls(EnemyID, player);
                    Slain[EnemyID] = 1;
                }
            }
        }

        public static void StaminaVesselOnFirstBag(int EnemyID, Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(EnemyID))
            {
                if (Slain[EnemyID] == 0)
                {
                    player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<StaminaVessel>());
                    //Don't set slain to 1, let SoulsOnFirstBag do that as they all run it
                }
            }
        }

        public static void EstusFlaskShardOnFirstBag(int EnemyID, Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(EnemyID))
            {
                if (Slain[EnemyID] == 0 && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<EstusFlaskShard>());
                    //Don't set slain to 1, let SoulsOnFirstBag do that as they all run it
                }
            }
        }

        public static void SublimeBoneDustOnFirstBag(int EnemyID, Player player)
        {
            var Slain = tsorcRevampWorld.Slain;
            if (Slain.ContainsKey(EnemyID))
            {
                if (Slain[EnemyID] == 0 && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<SublimeBoneDust>());
                    //Don't set slain to 1, let SoulsOnFirstBag do that as they all run it
                }
            }
        }
        // [System.Obsolete]
        // public override bool PreOpenVanillaBag(string context, Player player, int arg)
        // {

        //     if (context == "bossBag" && arg == ItemID.KingSlimeBossBag)
        //     { //re-implement king slime bag to stop blacklisted items from dropping in adventure mode
        //         player.QuickSpawnItem(player.GetSource_Loot(), ItemID.RoyalGel);
        //         player.QuickSpawnItem(player.GetSource_Loot(), ItemID.Solidifier);
        //         player.QuickSpawnItem(player.GetSource_Loot(), ItemID.GoldCoin, 11);
        //         player.QuickSpawnItem(player.GetSource_Loot(), ItemID.Katana);
        //         if (Main.rand.Next(99) < 66) { player.QuickSpawnItem(player.GetSource_Loot(), ItemID.NinjaHood); }
        //         if (Main.rand.Next(99) < 66) { player.QuickSpawnItem(player.GetSource_Loot(), ItemID.NinjaShirt); }
        //         if (Main.rand.Next(99) < 66) { player.QuickSpawnItem(player.GetSource_Loot(), ItemID.NinjaPants); }
        //         if (Main.rand.NextBool(7)) { player.QuickSpawnItem(player.GetSource_Loot(), ItemID.KingSlimeMask); }
        //         if (Main.rand.NextBool(10)) { player.QuickSpawnItem(player.GetSource_Loot(), ItemID.KingSlimeTrophy); }
        //         if (Main.rand.NextBool(2)) { player.QuickSpawnItem(player.GetSource_Loot(), ItemID.SlimeGun); }
        //         //player.QuickSpawnItem(player.GetSource_Loot(), ItemID.SlimySaddle); //didn't want such a powerful mobility tool obtainable from such a relatively easy, early-game mini-boss; also wanted sequence breaking to be done via the map if they found it rather than vanilla terraria knowledge
        //         if (Main.rand.NextBool(2)) { player.QuickSpawnItem(player.GetSource_Loot(), ItemID.SlimeHook); }

        //         StaminaVesselOnFirstBag(NPCID.KingSlime, player);
        //         SoulsOnFirstBag(NPCID.KingSlime, player);
        //         return false;
        //     }
        //     if (context == "bossBag" && arg == ItemID.GolemBossBag)
        //     {
        //         //Picksaw drops from Attraidies who is Post-Golem now, and gates SuperHardMode content. We've gotta stop Golem from dropping it.
        //         if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
        //         {
        //             if (Main.rand.NextBool(3)) { player.QuickSpawnItem(player.GetSource_Loot(), ItemID.Picksaw); }
        //         }
        //         else
        //         {
        //             player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<Items.BrokenPicksaw>());
        //         }

        //         //Drops that work in the traditional way. Also, adds the Crest of Stone to its drops.
        //         player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<CrestOfStone>());
        //         player.QuickSpawnItem(player.GetSource_Loot(), ItemID.ShinyStone);
        //         if (Main.rand.NextBool(6)) { player.QuickSpawnItem(player.GetSource_Loot(), ItemID.GolemMask); }
        //         if (Main.rand.NextBool(9)) { player.QuickSpawnItem(player.GetSource_Loot(), ItemID.GolemTrophy); }
        //         player.QuickSpawnItem(player.GetSource_Loot(), ItemID.GreaterHealingPotion, 5 + Main.rand.Next(10));

        //         //Always drops one of these things, picked at random
        //         int drop = Main.rand.Next(6);
        //         switch (drop)
        //         {
        //             case 0:
        //                 player.QuickSpawnItem(player.GetSource_Loot(), ItemID.Stynger);
        //                 player.QuickSpawnItem(player.GetSource_Loot(), ItemID.StyngerBolt, 60 + Main.rand.Next(39));
        //                 break;
        //             case 1:
        //                 player.QuickSpawnItem(player.GetSource_Loot(), ItemID.PossessedHatchet);
        //                 break;
        //             case 2:
        //                 player.QuickSpawnItem(player.GetSource_Loot(), ItemID.SunStone);
        //                 break;
        //             case 3:
        //                 player.QuickSpawnItem(player.GetSource_Loot(), ItemID.EyeoftheGolem);
        //                 break;
        //             case 4:
        //                 player.QuickSpawnItem(player.GetSource_Loot(), ItemID.HeatRay);
        //                 break;
        //             case 5:
        //                 player.QuickSpawnItem(player.GetSource_Loot(), ItemID.StaffofEarth);
        //                 break;
        //             case 6:
        //                 player.QuickSpawnItem(player.GetSource_Loot(), ItemID.GolemFist);
        //                 break;
        //         }

        //         SoulsOnFirstBag(NPCID.Golem, player);
        //         return false;
        //     }



        //     return base.PreOpenVanillaBag(context, player, arg);
        // }
        // public override void OpenVanillaBag(string context, Player player, int arg)
        // {
        //     var Slain = tsorcRevampWorld.Slain;
        //     if (context == "bossBag")
        //     {
        //         if (arg == ItemID.EyeOfCthulhuBossBag)
        //         {
        //             UsefulFunctions.BroadcastText("Open", Color.Cyan);
        //             player.QuickSpawnItem(player.GetSource_Loot(), ItemID.HermesBoots);
        //             player.QuickSpawnItem(player.GetSource_Loot(), ItemID.HerosHat);
        //             player.QuickSpawnItem(player.GetSource_Loot(), ItemID.HerosPants);
        //             player.QuickSpawnItem(player.GetSource_Loot(), ItemID.HerosShirt);
        //             SublimeBoneDustOnFirstBag(NPCID.EyeofCthulhu, player);
        //             SoulsOnFirstBag(NPCID.EyeofCthulhu, player);
        //         }
        //         if (arg == ItemID.EaterOfWorldsBossBag)
        //         {
        //             SoulsOnFirstBag(NPCID.EaterofWorldsHead, player);
        //         }
        //         if (arg == ItemID.BrainOfCthulhuBossBag)
        //         {
        //             StaminaVesselOnFirstBag(NPCID.BrainofCthulhu, player);
        //             SoulsOnFirstBag(NPCID.BrainofCthulhu, player);
        //         }
        //         if (arg == ItemID.QueenBeeBossBag)
        //         {
        //             if (Slain.ContainsKey(NPCID.QueenBee))
        //             {
        //                 if (Slain[NPCID.QueenBee] == 0)
        //                 {
        //                     VanillaBossBag.AddBossBagSouls(NPCID.QueenBee, player, false, true);
        //                     Slain[NPCID.QueenBee] = 1;
        //                 }
        //             };
        //         }
        //         if (arg == ItemID.WallOfFleshBossBag)
        //         {
        //             EstusFlaskShardOnFirstBag(NPCID.WallofFlesh, player);
        //             SoulsOnFirstBag(NPCID.WallofFlesh, player);
        //         }
        //         if (arg == ItemID.SkeletronBossBag)
        //         {
        //             SublimeBoneDustOnFirstBag(NPCID.SkeletronHead, player);
        //             SoulsOnFirstBag(NPCID.SkeletronHead, player);
        //             player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<MiakodaFull>());
        //         }
        //         if (arg == ItemID.DestroyerBossBag)
        //         {
        //             SoulsOnFirstBag(NPCID.TheDestroyer, player);
        //             player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<RTQ2>());
        //             player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<CrestOfCorruption>(), 1);
        //         }
        //         if (arg == ItemID.TwinsBossBag)
        //         {
        //             /* 
        //             * picture the following:
        //             * Twins are killed. Spazmatism is added to Slain, and the player opens a bag and receives souls
        //             * then, Twins are killed again. Retinazer is added to slain this time, and the player opens a bag and gets souls again
        //             * to prevent this, we need to make sure we haven't opened a bag from Spazmatism when we open a bag in Retinazer's context
        //             */
        //             if (Slain.ContainsKey(NPCID.Retinazer))
        //             {
        //                 if (Slain[NPCID.Retinazer] == 0)
        //                 {
        //                     bool SpazmatismDowned = Slain.TryGetValue(NPCID.Spazmatism, out int value);
        //                     //if SpazmatismDowned evaluates to true, int value is set to the value pair of Spazmatism's key, which stores if a bag has been opened
        //                     if (!SpazmatismDowned || value == 0)
        //                     { //if Spazmatism is not in Slain, or no twins bag has been opened in Spazmatism's context
        //                         AddBossBagSouls(NPCID.Retinazer, player);
        //                         Slain[NPCID.Retinazer] = 1;
        //                     }
        //                 }
        //             }
        //             else if (Slain.ContainsKey(NPCID.Spazmatism))
        //             { //dont need to check if Retinazer is downed, since this is only run if Retinazer is not in Slain
        //                 if (Slain[NPCID.Spazmatism] == 0)
        //                 {
        //                     AddBossBagSouls(NPCID.Spazmatism, player);
        //                     Slain[NPCID.Spazmatism] = 1;
        //                 }
        //             }
        //             player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<CrestOfSky>(), 1);
        //         }
        //         if (arg == ItemID.SkeletronPrimeBossBag)
        //         {
        //             SublimeBoneDustOnFirstBag(NPCID.SkeletronPrime, player);
        //             SoulsOnFirstBag(NPCID.SkeletronPrime, player);
        //             player.QuickSpawnItem(player.GetSource_Loot(), ItemID.AngelWings);
        //             player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<CrestOfSteel>(), 1);
        //         }
        //         if (arg == ItemID.PlanteraBossBag)
        //         {
        //             player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<CrestOfLife>());
        //             player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<SoulOfLife>(), 3);
        //             SoulsOnFirstBag(NPCID.Plantera, player);
        //         }
        //         if (arg == ItemID.FishronBossBag)
        //         {
        //             StaminaVesselOnFirstBag(NPCID.DukeFishron, player);
        //             SoulsOnFirstBag(NPCID.DukeFishron, player);
        //         }
        //         if (arg == ItemID.BossBagBetsy)
        //         {
        //             SoulsOnFirstBag(NPCID.DD2Betsy, player);
        //         }
        //         if (arg == ItemID.MoonLordBossBag)
        //         {
        //             if (Slain.ContainsKey(NPCID.MoonLordCore))
        //             {
        //                 if (Slain[NPCID.MoonLordCore] == 0)
        //                 {
        //                     SoulsOnFirstBag(NPCID.MoonLordCore, player); //idk why but there was a lot of seemingly unnecessary code here, Moon Lord will just drop his souls according to his Cores value like this once
        //                 }
        //             }
        //         }
        //         if (arg == ItemID.QueenSlimeBossBag)
        //         {
        //             if (Slain.ContainsKey(NPCID.QueenSlimeBoss))
        //             {
        //                 if (Slain[NPCID.QueenSlimeBoss] == 0)
        //                 {
        //                     VanillaBossBag.AddBossBagSouls(NPCID.QueenSlimeBoss, player, false, true);
        //                     Slain[NPCID.QueenSlimeBoss] = 1;
        //                 }
        //             };
        //         }
        //         if (arg == ItemID.FairyQueenBossBag)
        //         {
        //             if (Slain.ContainsKey(NPCID.HallowBoss))
        //             {
        //                 if (Slain[NPCID.HallowBoss] == 0)
        //                 {
        //                     VanillaBossBag.AddBossBagSouls(NPCID.HallowBoss, player, false, true);
        //                     Slain[NPCID.HallowBoss] = 1;
        //                 }
        //             };
        //         }
        //         if (arg == ItemID.BossBagBetsy)
        //         {
        //             if (Slain.ContainsKey(NPCID.DD2Betsy))
        //             {
        //                 if (Slain[NPCID.DD2Betsy] == 0)
        //                 {
        //                     VanillaBossBag.AddBossBagSouls(NPCID.DD2Betsy, player, false, true);
        //                     Slain[NPCID.DD2Betsy] = 1;
        //                 }
        //             };
        //         }
        //         if (arg == ItemID.DeerclopsBossBag)
        //         {
        //             if (Slain.ContainsKey(NPCID.Deerclops))
        //             {
        //                 if (Slain[NPCID.Deerclops] == 0)
        //                 {
        //                     VanillaBossBag.AddBossBagSouls(NPCID.Deerclops, player, false, true);
        //                     Slain[NPCID.Deerclops] = 1;
        //                 }
        //             };
        //         }
        //     }
        // }
    }
}
