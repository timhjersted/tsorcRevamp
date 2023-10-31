using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Expert;
using tsorcRevamp.Items.Accessories.Mobility;
using tsorcRevamp.Items.BossItems;
using tsorcRevamp.Items.Lore;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Potions.PermanentPotions;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.Items.Vanity;
using tsorcRevamp.Items.Weapons.Magic;
using tsorcRevamp.Items.Weapons.Magic.Tomes;
using tsorcRevamp.Items.Weapons.Melee.Shortswords;

namespace tsorcRevamp.Items.BossBags
{
    public abstract class BossBag : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag");
            ItemID.Sets.BossBag[Type] = true;
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
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
            for (int i = 0; i < 4; ++i)
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
            for (int i = 0; i < 4; ++i)
            {
                Vector2 offsetPositon = Vector2.UnitY.RotatedBy(((Main.GameUpdateCount % 300) / 30f) + MathHelper.PiOver2 * i) * 5;
                spriteBatch.Draw(texture, offsetPositon + new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), Main.DiscoColor, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }

            return true;
        }
    }

    #region PreHardMode

    public class OolacileDemonBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Ancient Oolacile Demon)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.DragonCrestShield>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.BandOfCosmicPower>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PermanentShinePotion>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PermanentNightOwlPotion>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ImprovedBlueBalloon>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 1, 3, 6));
        }
    }
    public class SlograBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Slogra)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.BurningStone>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.PoisonbiteRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.BloodbiteRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Ranged.DarkTrident>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Summon.SunsetQuasar>()));
        }
    }
    public class GaibonBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Gaibon)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.BurningAura>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.PoisonbiteRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.BloodbiteRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Ranged.DarkTrident>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Summon.SunsetQuasar>()));
        }
    }
    public class JungleWyvernBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Jungle Wyvern)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
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
    }
    public class AncestralSpiritBag : BossBag
    {
        public override void SetStaticDefaults()
        {
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ItemID.BoneHelm));
            itemLoot.Add(ItemDropRule.Common(ItemID.ChesterPetItem));
            itemLoot.Add(ItemDropRule.Common(ItemID.Eyebrella));
            itemLoot.Add(ItemDropRule.Common(ItemID.DontStarveShaderItem));
            itemLoot.Add(ItemDropRule.Common(ItemID.DizzyHat));
            itemLoot.Add(ItemDropRule.Common(ItemID.LucyTheAxe));
            itemLoot.Add(ItemDropRule.Common(ItemID.WeatherPain));
            itemLoot.Add(ItemDropRule.Common(ItemID.PewMaticHorn));
            itemLoot.Add(ItemDropRule.Common(ItemID.HoundiusShootius));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<AncestralSpiritMask>(), 7));
        }
    }
    public class AncientDemonBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Ancient Demon)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PermanentSoulSiphonPotion>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.CrackedDragonStone>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.EyeOfTheGods>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.BarrierRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 1, 2, 4));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 1, 2, 4));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 1, 2, 4));
        }
    }
    #endregion

    #region Hardmode

    public class HeroOfLumeliaBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Hero of Lumelia)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.CovetousSilverSerpentRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 1, 15, 30));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Ammo.ArrowOfBard>(), 1, 15, 30));
            itemLoot.Add(ItemDropRule.Common(ItemID.WaterWalkingBoots));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<MagicBarrierScroll>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>()));
            itemLoot.Add(ItemDropRule.Common(ItemID.ObsidianSkinPotion));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 1, 2, 5));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 1, 2, 5));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<GreaterRestorationPotion>(), 1, 2, 5));
            itemLoot.Add(ItemDropRule.Common(ItemID.LifeforcePotion, 1, 2, 5));
        }
    }
    public class TheRageBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (The Rage)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PhoenixSkull>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Summon.PhoenixEgg>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrestOfFire>()));
            itemLoot.Add(ItemDropRule.Common(ItemID.CobaltDrill));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<TheRageMask>(), 7));
        }
    }
    public class TheSorrowBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (The Sorrow)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.GoldenHairpin>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrestOfWater>()));
            itemLoot.Add(ItemDropRule.Common(ItemID.AdamantiteDrill));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<TheSorrowMask>(), 7));
        }
    }
    public class TheHunterBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (The Hunter)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.MythrilBulwark>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrestOfEarth>()));
            itemLoot.Add(ItemDropRule.Common(ItemID.Drax));
            itemLoot.Add(ItemDropRule.Common(ItemID.AngelWings));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<TheHunterMask>(), 7));
        }
    }

    //TODO: Add a shader to make it glow in three colors
    public class TriadBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (The Triad)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.AuraOfIlluminance>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrestOfSky>(), 1, 3, 3));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DamagedCrystal>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DamagedFlameNozzle>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DamagedLaser>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DamagedRemote>()));
            itemLoot.Add(ItemDropRule.Common(ItemID.MechanicalWheelPiece));
            itemLoot.Add(ItemDropRule.Common(ItemID.HallowedBar, 1, 20, 35));
            itemLoot.Add(ItemDropRule.Common(ItemID.SoulofSight, 1, 25, 40));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<TheTriadMask>(), 7));
        }
    }
    public class TheMachineBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (The Triad)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.IonicFury>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrestOfSteel>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DamagedMechanicalScrap>()));
            itemLoot.Add(ItemDropRule.Common(ItemID.MechanicalBatteryPiece));
            itemLoot.Add(ItemDropRule.Common(ItemID.HallowedBar, 1, 20, 35));
            itemLoot.Add(ItemDropRule.Common(ItemID.SoulofFright, 1, 25, 40));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<TheMachineMask>(), 7));
        }
    }
    public class WyvernMageBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Wyvern Mage)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.LionheartGunblade>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Magic.GemBox>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<LampTome>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HolyWarElixir>(), 1, 2, 2));
        }
    }
    public class SerrisBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Serris)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert-exclusive item yet
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DemonDrugPotion>(), 1, 3, 7));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ArmorDrugPotion>(), 1, 3, 7));
        }
    }
    public class DeathBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Death)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.HerosCrest>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<GreatMagicShieldScroll>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<MagicBarrierScroll>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Laevateinn>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HolyWarElixir>(), 1, 4, 4));
        }
    }
    public class MindflayerIllusionBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Mindflayer Illusion)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //won't get an expert-exclusive item, just a part of Attraidies
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<MindflayerIllusionRelic>()));
        }
    }
    public class AttraidiesBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Attraidies)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.AuraOfDecay>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloomShards>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Summon.ShatteredReflection>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Broadswords.SeveringDusk>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Ranged.Guns.PyroclasticFlow>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HeavenPiercer>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<TheEnd>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulOfAttraidies>(), 1, 15, 23));
        }
    }
    #endregion

    #region SuperHardMode
    public class KrakenBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Water Fiend Kraken)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.DragoonHorn>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Shortswords.BarrowBlade>()));
        }
    }
    public class MarilithBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Fire Fiend Marilith)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert-exclusive item yet
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.ForgottenRisingSun>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<FairyInABottle>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HolyWarElixir>()));
        }
    }
    public class LichBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Earth Fiend Lich)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.DragoonBoots>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Broadswords.ForgottenGaiaSword>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HolyWarElixir>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<LichBone>(), 1, 3, 6));
        }
    }
    public class BlightBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Blight)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert-exclusive item yet
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DivineSpark>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulOfBlight>(), 1, 3, 6));
        }
    }
    public class ChaosBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Chaos)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert-exclusive item yet
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<FlareTome>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Ranged.Bows.ElfinBow>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HolyWarElixir>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulOfChaos>(), 1, 3, 6));
        }
    }
    public class WyvernMageShadowBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Wyvern Mage Shadow)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.RingOfPower>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.HolyWarElixir>(), 1, 4, 4));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<GhostWyvernSoul>(), 1, 8, 8));
        }
    }
    public class OolacileSorcererBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Abysmal Oolacile Sorcerer)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.DuskCrownRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Potions.HealingElixir>(), 1, 10, 20));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PurgingStone>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 2, 4));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 5, 10));
        }
    }
    public class ArtoriasBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Artorias)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.RingofArtorias>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.WolfRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulOfArtorias>(), 1, 6, 12));
        }
    }
    public class HellkiteBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Hellkite Dragon)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.DragonStone>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HellkiteStone>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Spears.HiRyuuSpear>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DragonEssence>(), 1, 22, 28));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 5, 10));
        }
    }
    public class SeathBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Seath the Scaleless)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.WingsOfSeath>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.BlueTearstoneRing>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PurgingStone>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DragonEssence>(), 1, 35, 50));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BequeathedSoul>(), 1, 3, 6));
        }
    }
    public class WitchkingBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Witchking)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.MorgulBlade>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Broadswords.WitchkingsSword>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BewitchedTitanite>(), 1, 20, 30));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Defensive.CovenantOfArtorias>()));
        }
    }
    public class DarkCloudBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Dark Cloud)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Accessories.Expert.ReflectionShift>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Melee.Broadswords.MoonlightGreatsword>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.Summon.NullSpriteStaff>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<GuardianSoul>(), 1, 5, 10));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 3, 6));
        }
    }
    public class GwynBag : BossBag
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Treasure Bag (Gwyn, Lord of Cinder)");
            // Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
        }
        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            //no expert-exclusive item yet
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Epilogue>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<EssenceOfTerraria>()));
            itemLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<DraxEX>()));
        }
    }
    #endregion

    public class VanillaBossBag : GlobalItem
    {
        public static void GiveDarkSouls(int bossBagID, Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.bagsOpened.Contains(bossBagID))
            {
                return;
            }
            NPC boss = new NPC();
            boss.SetDefaults(tsorcRevamp.BossBagIDtoNPCID[bossBagID]);
            float bossValue = boss.value / 25f;
            if (Main.masterMode)
            {
                bossValue *= 1.2f;
            }
            float multiplier = tsorcRevampPlayer.CheckSoulsMultiplier(player);
            player.QuickSpawnItem(player.GetSource_Loot(), ModContent.ItemType<DarkSoul>(), (int)(multiplier * bossValue));
            modPlayer.bagsOpened.Add(bossBagID);
        }

        public override void RightClick(Item item, Player player)
        {
            // check if an item is a Treasure Bag
            if (!tsorcRevamp.BossBagIDtoNPCID.ContainsKey(item.type))
            {
                return;
            }
            GiveDarkSouls(item.type, player);
        }

        public override void ModifyItemLoot(Item item, ItemLoot loot)
        {
            // check if an item is a Treasure Bag
            if (!tsorcRevamp.BossBagIDtoNPCID.ContainsKey(item.type))
            {
                return;
            }

            int itemID = item.type;

            // take into account blocked items
            if (tsorcRevamp.RemovedBossBagLoot.ContainsKey(itemID))
            {
                List<IItemDropRule> dropRules = loot.Get();
                foreach (var rule in dropRules)
                {
                    List<int> ruleItems = new List<int>() { };
                    if (rule is CommonDrop)
                    {
                        ruleItems.Add(((CommonDrop)rule).itemId);
                    }
                    else if (rule is DropOneByOne)
                    {
                        ruleItems.Add(((DropOneByOne)rule).itemId);
                    }
                    else if (rule is OneFromOptionsDropRule)
                    {
                        foreach (var dropId in ((OneFromOptionsDropRule)rule).dropIds)
                        {
                            ruleItems.Add(dropId);
                        }
                    }
                    else if (rule is OneFromOptionsNotScaledWithLuckDropRule)
                    {
                        foreach (var dropId in ((OneFromOptionsNotScaledWithLuckDropRule)rule).dropIds)
                        {
                            ruleItems.Add(dropId);
                        }
                    }
                    else
                    {
                        continue;
                    }

                    foreach (var itemToRemove in tsorcRevamp.RemovedBossBagLoot[itemID])
                    {
                        if (ruleItems.Contains(itemToRemove))
                        {
                            loot.Remove(rule);
                            continue;
                        }
                    }
                }
            }

            // add needed extras to Treasure Bags
            tsorcRevamp.BossExtras assignedExtras = tsorcRevamp.AssignedBossExtras[itemID];
            foreach (var it in tsorcRevamp.BossExtrasDescription)
            {
                if ((assignedExtras & it.Key) != 0)
                {
                    loot.Add(ItemDropRule.ByCondition(it.Value.Condition, it.Value.ID));
                }
            }

            // add other extra items to Treasure Bags
            if (tsorcRevamp.AddedBossBagLoot.ContainsKey(itemID))
            {
                foreach (var dropRule in tsorcRevamp.AddedBossBagLoot[itemID])
                {
                    loot.Add(dropRule);
                }
            }
        }
    }
}
