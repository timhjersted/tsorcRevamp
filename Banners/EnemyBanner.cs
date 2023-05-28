using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using tsorcRevamp.NPCs.Bosses;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;
using tsorcRevamp.NPCs.Friendly;

namespace tsorcRevamp.Banners
{
    public abstract class EnemyBanner : ModItem
    {
        public abstract int PlaceStyle { get; }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<EnemyBannerTile>(), PlaceStyle);
            Item.width = 10;
            Item.height = 24;
            Item.scale = 1.5f;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(0, 0, 10, 0);
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            scale = 1.5f;
            return true;
        }
    }

    public class EnemyBannerTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.SolidBottom, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.StyleWrapLimit = 111;
            TileObjectData.addTile(Type);

            TileID.Sets.DisableSmartCursor[Type] = true;

            AddMapEntry(new Color(13, 88, 130), Language.GetText("MapObject.Banner"));
        }

        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            int style = Main.tile[i, j].TileFrameX / 18;
            int dropItem = TileLoader.GetItemDropFromTypeAndStyle(Type, style);
            yield return new Item(dropItem);
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (closer)
            {
                int style = Main.tile[i, j].TileFrameX / 18;
                int type = style switch
                {
                    0 => ModContent.NPCType<GuardianCorruptor>(),
                    1 => ModContent.NPCType<CloudBat>(),
                    2 => ModContent.NPCType<ArmoredWraith>(),
                    3 => ModContent.NPCType<ObsidianJellyfish>(),
                    4 => ModContent.NPCType<StoneGolem>(),
                    5 => ModContent.NPCType<AbandonedStump>(),
                    6 => ModContent.NPCType<ResentfulSeedling>(),
                    7 => ModContent.NPCType<LivingShroom>(),
                    8 => ModContent.NPCType<LivingShroomThief>(),
                    9 => ModContent.NPCType<LivingGlowshroom>(),
                    10 => ModContent.NPCType<AncientDemon>(),
                    11 => ModContent.NPCType<UndeadCaster>(),
                    12 => ModContent.NPCType<Chicken>(),
                    13 => ModContent.NPCType<AttraidiesIllusion>(),
                    14 => ModContent.NPCType<CosmicCrystalLizard>(),
                    15 => ModContent.NPCType<Assassin>(),
                    16 => ModContent.NPCType<AttraidiesManifestation>(),
                    17 => ModContent.NPCType<BarrowWight>(),
                    18 => ModContent.NPCType<BasiliskShifter>(),
                    19 => ModContent.NPCType<BasiliskWalker>(),
                    20 => ModContent.NPCType<BlackKnight>(),
                    21 => ModContent.NPCType<CrazedDemonSpirit>(),
                    22 => ModContent.NPCType<DarkElfMage>(),
                    23 => ModContent.NPCType<DemonSpirit>(),
                    24 => ModContent.NPCType<DiscipleOfAttraidies>(),
                    25 => ModContent.NPCType<DungeonMage>(),
                    26 => ModContent.NPCType<Dunlending>(),
                    27 => ModContent.NPCType<DworcFleshhunter>(),
                    28 => ModContent.NPCType<DworcVenomsniper>(),
                    29 => ModContent.NPCType<DworcVoodoomaster>(),
                    30 => ModContent.NPCType<DworcVoodooShaman>(),
                    31 => ModContent.NPCType<FirebombHollow>(),
                    32 => ModContent.NPCType<FlameBat>(),
                    33 => ModContent.NPCType<GhostOfTheDarkmoonKnight>(),
                    34 => ModContent.NPCType<GhostoftheForgottenKnight>(),
                    35 => ModContent.NPCType<GhostOfTheForgottenWarrior>(),
                    36 => ModContent.NPCType<FireLurker>(),
                    // 37 => ModContent.NPCType<HeroOfLumelia>(), 
                    38 => ModContent.NPCType<JungleSentree>(),
                    39 => ModContent.NPCType<ManHunter>(),
                    40 => ModContent.NPCType<MarilithSpiritTwin>(),
                    41 => ModContent.NPCType<MindflayerIllusion>(),
                    42 => ModContent.NPCType<MindflayerKingServant>(),
                    43 => ModContent.NPCType<MindflayerServant>(),
                    44 => ModContent.NPCType<MutantToad>(),
                    45 => ModContent.NPCType<Necromancer>(),
                    46 => ModContent.NPCType<NecromancerElemental>(),
                    47 => ModContent.NPCType<Parasprite>(),
                    48 => ModContent.NPCType<QuaraHydromancer>(),
                    49 => ModContent.NPCType<RedCloudHunter>(),
                    50 => ModContent.NPCType<HollowSoldier>(),
                    51 => ModContent.NPCType<ShadowMage>(),
                    52 => ModContent.NPCType<SnowOwl>(),
                    53 => ModContent.NPCType<TibianAmazon>(),
                    54 => ModContent.NPCType<TibianValkyrie>(),
                    55 => ModContent.NPCType<Tonberry>(),
                    56 => ModContent.NPCType<Warlock>(),
                    57 => ModContent.NPCType<WaterSpirit>(),
                    58 => ModContent.NPCType<ZombieWormHead>(),
                    59 => ModContent.NPCType<NPCs.Enemies.JungleWyvernJuvenile.JungleWyvernJuvenileHead>()
                    60 => ModContent.NPCType<NPCs.Enemies.SuperHardMode.SerpentOfTheAbyss.SerpentOfTheAbyssHead>(),
                    61 => ModContent.NPCType<Abysswalker>(),
                    62 => ModContent.NPCType<AncientDemonOfTheAbyss>(),
                    63 => ModContent.NPCType<BarrowWightNemesis>(),
                    64 => ModContent.NPCType<BarrowWightPhantom>(),
                    65 => ModContent.NPCType<BasiliskHunter>(),
                    66 => ModContent.NPCType<CorruptedElemental>(),
                    67 => ModContent.NPCType<CorruptedHornet>(),
                    68 => ModContent.NPCType<CrystalKnight>(),
                    // 69 => REMOVED,
                    70 => ModContent.NPCType<DarkBloodKnight>(),
                    71 => ModContent.NPCType<DarkKnight>(),
                    72 => ModContent.NPCType<HollowWarrior>(),
                    73 => ModContent.NPCType<HydrisElemental>(),
                    74 => ModContent.NPCType<HydrisNecromancer>(),
                    75 => ModContent.NPCType<IceSkeleton>(),
                    76 => ModContent.NPCType<ManOfWar>(),
                    77 => ModContent.NPCType<OolacileDemon>(),
                    78 => ModContent.NPCType<OolacileKnight>(),
                    79 => ModContent.NPCType<OolacileSorcerer>(),
                    80 => ModContent.NPCType<SlograII>(),
                    81 => ModContent.NPCType<TaurusKnight>(),
                    82 => ModContent.NPCType<Tetsujin>(),
                    83 => ModContent.NPCType<VampireBat>(),
                    84 => ModContent.NPCType<Archdeacon>(),
                    85 => ModContent.NPCType<RedKnight>(),
                    86 => ModContent.NPCType<GreatRedKnight>(),
                    _ => NPCID.None,
                };

                if (type == NPCID.None)
                {
                    return;
                }

                Main.SceneMetrics.NPCBannerBuff[type] = true;
                Main.SceneMetrics.hasBanner = true;
            }
        }

        public override void SetSpriteEffects(int i, int j, ref SpriteEffects spriteEffects)
        {
            if (i % 2 == 1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
        }
    }


    #region Banner Classes

    public class GuardianCorruptorBanner : EnemyBanner
    {
        public override int PlaceStyle => 0;
    }

    public class CloudBatBanner : EnemyBanner
    {
        public override int PlaceStyle => 1;
    }

    public class ArmoredWraithBanner : EnemyBanner
    {
        public override int PlaceStyle => 2;
    }

    public class ObsidianJellyfishBanner : EnemyBanner
    {
        public override int PlaceStyle => 3;
    }

    public class StoneGolemBanner : EnemyBanner
    {
        public override int PlaceStyle => 4;
    }

    public class AbandonedStumpBanner : EnemyBanner
    {
        public override int PlaceStyle => 5;
    }

    public class ResentfulSeedlingBanner : EnemyBanner
    {
        public override int PlaceStyle => 6;
    }

    public class LivingShroomBanner : EnemyBanner
    {
        public override int PlaceStyle => 7;
    }

    public class LivingShroomThiefBanner : EnemyBanner
    {
        public override int PlaceStyle => 8;
    }

    public class LivingGlowshroomBanner : EnemyBanner
    {
        public override int PlaceStyle => 9;
    }

    public class AncientDemonBanner : EnemyBanner
    {
        public override int PlaceStyle => 10;
    }

    public class UndeadCasterBanner : EnemyBanner
    {
        public override int PlaceStyle => 11;
    }

    public class ChickenBanner : EnemyBanner
    {
        public override int PlaceStyle => 12;
    }

    public class AttraidiesIllusionBanner : EnemyBanner
    {
        public override int PlaceStyle => 13;
    }

    public class CosmicCrystalLizardBanner : EnemyBanner
    {
        public override int PlaceStyle => 14;
    }

    public class AssassinBanner : EnemyBanner
    {
        public override int PlaceStyle => 15;
    }

    public class AttraidiesManifestationBanner : EnemyBanner
    {
        public override int PlaceStyle => 16;
    }

    public class BarrowWightBanner : EnemyBanner
    {
        public override int PlaceStyle => 17;
    }

    public class BasiliskShifterBanner : EnemyBanner
    {
        public override int PlaceStyle => 18;
    }

    public class BasiliskWalkerBanner : EnemyBanner
    {
        public override int PlaceStyle => 19;
    }

    public class BlackKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 20;
    }

    public class CrazedDemonSpiritBanner : EnemyBanner
    {
        public override int PlaceStyle => 21;
    }

    public class DarkElfMageBanner : EnemyBanner
    {
        public override int PlaceStyle => 22;
    }

    public class DemonSpiritBanner : EnemyBanner
    {
        public override int PlaceStyle => 23;
    }

    public class DiscipleOfAttraidiesBanner : EnemyBanner
    {
        public override int PlaceStyle => 24;
    }

    public class DungeonMageBanner : EnemyBanner
    {
        public override int PlaceStyle => 25;
    }

    public class DunlendingBanner : EnemyBanner
    {
        public override int PlaceStyle => 26;
    }

    public class DworcFleshhunterBanner : EnemyBanner
    {
        public override int PlaceStyle => 27;
    }

    public class DworcVenomsniperBanner : EnemyBanner
    {
        public override int PlaceStyle => 28;
    }

    public class DworcVoodoomasterBanner : EnemyBanner
    {
        public override int PlaceStyle => 29;
    }

    public class DworcVoodooShamanBanner : EnemyBanner
    {
        public override int PlaceStyle => 30;
    }

    public class FirebombHollowBanner : EnemyBanner
    {
        public override int PlaceStyle => 31;
    }

    public class FlameBatBanner : EnemyBanner
    {
        public override int PlaceStyle => 32;
    }

    public class GhostOfTheDarkmoonKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 33;
    }

    public class GhostOfTheForgottenKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 34;
    }

    public class GhostOfTheForgottenWarriorBanner : EnemyBanner
    {
        public override int PlaceStyle => 35;
    }

    public class FireLurkerBanner : EnemyBanner
    {
        public override int PlaceStyle => 36;
    }

    public class JungleSentreeBanner : EnemyBanner
    {
        public override int PlaceStyle => 38;
    }

    public class ManHunterBanner : EnemyBanner
    {
        public override int PlaceStyle => 39;
    }

    public class MarilithSpiritTwinBanner : EnemyBanner
    {
        public override int PlaceStyle => 40;
    }

    public class MindflayerIllusionBanner : EnemyBanner
    {
        public override int PlaceStyle => 41;
    }

    public class MindflayerKingServantBanner : EnemyBanner
    {
        public override int PlaceStyle => 42;
    }

    public class MindflayerServantBanner : EnemyBanner
    {
        public override int PlaceStyle => 43;
    }

    public class MutantToadBanner : EnemyBanner
    {
        public override int PlaceStyle => 44;
    }

    public class NecromancerBanner : EnemyBanner
    {
        public override int PlaceStyle => 45;
    }

    public class NecromancerElementalBanner : EnemyBanner
    {
        public override int PlaceStyle => 46;
    }

    public class ParaspriteBanner : EnemyBanner
    {
        public override int PlaceStyle => 47;
    }

    public class QuaraHydromancerBanner : EnemyBanner
    {
        public override int PlaceStyle => 48;
    }

    public class RedCloudHunterBanner : EnemyBanner
    {
        public override int PlaceStyle => 49;
    }

    public class HollowSoldierBanner : EnemyBanner
    {
        public override int PlaceStyle => 50;
    }

    public class ShadowMageBanner : EnemyBanner
    {
        public override int PlaceStyle => 51;
    }

    public class SnowOwlBanner : EnemyBanner
    {
        public override int PlaceStyle => 52;
    }

    public class TibianAmazonBanner : EnemyBanner
    {
        public override int PlaceStyle => 53;
    }

    public class TibianValkyrieBanner : EnemyBanner
    {
        public override int PlaceStyle => 54;
    }

    public class TonberryBanner : EnemyBanner
    {
        public override int PlaceStyle => 55;
    }

    public class WarlockBanner : EnemyBanner
    {
        public override int PlaceStyle => 56;
    }

    public class WaterSpiritBanner : EnemyBanner
    {
        public override int PlaceStyle => 57;
    }

    public class ParasyticWormBanner : EnemyBanner
    {
        public override int PlaceStyle => 58;
    }

    public class JungleWyvernJuvenileBanner : EnemyBanner
    {
        public override int PlaceStyle => 59;
    }

    public class SerpentOfTheAbyssBanner : EnemyBanner
    {
        public override int PlaceStyle => 60;
    }

    public class AbysswalkerBanner : EnemyBanner
    {
        public override int PlaceStyle => 61;
    }

    public class AncientDemonOfTheAbyssBanner : EnemyBanner
    {
        public override int PlaceStyle => 62;
    }

    public class BarrowWightNemesisBanner : EnemyBanner
    {
        public override int PlaceStyle => 63;
    }

    public class BarrowWightPhantomBanner : EnemyBanner
    {
        public override int PlaceStyle => 64;
    }

    // Backwards compatibility with the previously inconsistently named banner.
    [LegacyName("BasiliskHunter")]
    public class BasiliskHunterBanner : EnemyBanner
    {
        public override int PlaceStyle => 65;
    }

    public class CorruptedElementalBanner : EnemyBanner
    {
        public override int PlaceStyle => 66;
    }

    public class CorruptedHornetBanner : EnemyBanner
    {
        public override int PlaceStyle => 67;
    }

    public class CrystalKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 68;
    }

    public class DarkBloodKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 70;
    }

    public class DarkKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 71;
    }

    public class HollowWarriorBanner : EnemyBanner
    {
        public override int PlaceStyle => 72;
    }

    public class HydrisElementalBanner : EnemyBanner
    {
        public override int PlaceStyle => 73;
    }

    public class HydrisNecromancerBanner : EnemyBanner
    {
        public override int PlaceStyle => 74;
    }

    public class IceSkeletonBanner : EnemyBanner
    {
        public override int PlaceStyle => 75;
    }

    public class ManOfWarBanner : EnemyBanner
    {
        public override int PlaceStyle => 76;
    }

    public class OolacileDemonBanner : EnemyBanner
    {
        public override int PlaceStyle => 77;
    }

    public class OolacileKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 78;
    }

    public class OolacileSorcererBanner : EnemyBanner
    {
        public override int PlaceStyle => 79;
    }

    public class SlograIIBanner : EnemyBanner
    {
        public override int PlaceStyle => 80;
    }

    public class TaurusKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 81;
    }

    public class TetsujinBanner : EnemyBanner
    {
        public override int PlaceStyle => 82;
    }

    public class VampireBatBanner : EnemyBanner
    {
        public override int PlaceStyle => 83;
    }

    public class ArchdeaconBanner : EnemyBanner
    {
        public override int PlaceStyle => 84;
    }

    public class RedKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 85;
    }

    public class GreatRedKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 86;
    }

    public class HumanityPhantomBanner : EnemyBanner
    {
        public override int PlaceStyle => 87;
    }

    public class LothricBlackKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 88;
    }

    public class LothricKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 89;
    }

    public class LothricSpearKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 90;
    }

    public class RingedKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 91;
    }
    #endregion
}
