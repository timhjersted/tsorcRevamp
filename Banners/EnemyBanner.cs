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
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Enemies.ParasyticWorm;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;
using tsorcRevamp.NPCs.Friendly;

namespace tsorcRevamp.Banners
{
    public abstract class EnemyBanner : ModItem
    {
        public abstract int PlaceStyle { get; }

        public abstract int NPCType { get; }

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
                int itemType = TileLoader.GetItemDropFromTypeAndStyle(Type, style);
                var item = ContentSamples.ItemsByType[itemType].ModItem;
                if (item is EnemyBanner enemyBanner)
                {
                    Main.SceneMetrics.NPCBannerBuff[enemyBanner.NPCType] = true;
                    Main.SceneMetrics.hasBanner = true;
                }
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

        public override int NPCType => ModContent.NPCType<GuardianCorruptor>();
    }

    public class CloudBatBanner : EnemyBanner
    {
        public override int PlaceStyle => 1;

        public override int NPCType => ModContent.NPCType<CloudBat>();
    }

    public class ArmoredWraithBanner : EnemyBanner
    {
        public override int PlaceStyle => 2;

        public override int NPCType => ModContent.NPCType<ArmoredWraith>();
    }

    public class ObsidianJellyfishBanner : EnemyBanner
    {
        public override int PlaceStyle => 3;

        public override int NPCType => ModContent.NPCType<ObsidianJellyfish>();
    }

    public class StoneGolemBanner : EnemyBanner
    {
        public override int PlaceStyle => 4;

        public override int NPCType => ModContent.NPCType<StoneGolem>();
    }

    public class AbandonedStumpBanner : EnemyBanner
    {
        public override int PlaceStyle => 5;

        public override int NPCType => ModContent.NPCType<AbandonedStump>();
    }

    public class ResentfulSeedlingBanner : EnemyBanner
    {
        public override int PlaceStyle => 6;

        public override int NPCType => ModContent.NPCType<ResentfulSeedling>();
    }

    public class LivingShroomBanner : EnemyBanner
    {
        public override int PlaceStyle => 7;

        public override int NPCType => ModContent.NPCType<LivingShroom>();
    }

    public class LivingShroomThiefBanner : EnemyBanner
    {
        public override int PlaceStyle => 8;

        public override int NPCType => ModContent.NPCType<LivingShroomThief>();
    }

    public class LivingGlowshroomBanner : EnemyBanner
    {
        public override int PlaceStyle => 9;

        public override int NPCType => ModContent.NPCType<LivingGlowshroom>();
    }

    /*public class REDACTEDBanner : EnemyBanner
    {
        public override int PlaceStyle => 10;

        public override int NPCType => ModContent.NPCType<REDACTED>();
    }*/

    public class UndeadCasterBanner : EnemyBanner
    {
        public override int PlaceStyle => 11;

        public override int NPCType => ModContent.NPCType<UndeadCaster>();
    }

    /*public class REDACTEDBanner : EnemyBanner
    {
        public override int PlaceStyle => 12;

        public override int NPCType => ModContent.NPCType<REDACTED>();
    }*/

    public class AttraidiesIllusionBanner : EnemyBanner
    {
        public override int PlaceStyle => 13;

        public override int NPCType => ModContent.NPCType<AttraidiesIllusion>();
    }

    public class CosmicCrystalLizardBanner : EnemyBanner
    {
        public override int PlaceStyle => 14;

        public override int NPCType => ModContent.NPCType<CosmicCrystalLizard>();
    }

    public class AssassinBanner : EnemyBanner
    {
        public override int PlaceStyle => 15;

        public override int NPCType => ModContent.NPCType<Assassin>();
    }

    public class AttraidiesManifestationBanner : EnemyBanner
    {
        public override int PlaceStyle => 16;

        public override int NPCType => ModContent.NPCType<AttraidiesManifestation>();
    }

    public class BarrowWightBanner : EnemyBanner
    {
        public override int PlaceStyle => 17;

        public override int NPCType => ModContent.NPCType<BarrowWight>();
    }

    public class BasiliskShifterBanner : EnemyBanner
    {
        public override int PlaceStyle => 18;

        public override int NPCType => ModContent.NPCType<BasiliskShifter>();
    }

    public class BasiliskWalkerBanner : EnemyBanner
    {
        public override int PlaceStyle => 19;

        public override int NPCType => ModContent.NPCType<BasiliskWalker>();
    }

    public class BlackKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 20;

        public override int NPCType => ModContent.NPCType<BlackKnight>();
    }

    public class CrazedDemonSpiritBanner : EnemyBanner
    {
        public override int PlaceStyle => 21;

        public override int NPCType => ModContent.NPCType<CrazedDemonSpirit>();
    }

    public class ClericOfSorrowBanner : EnemyBanner
    {
        public override int PlaceStyle => 22;

        public override int NPCType => ModContent.NPCType<ClericOfSorrow>();
    }

    public class DemonSpiritBanner : EnemyBanner
    {
        public override int PlaceStyle => 23;

        public override int NPCType => ModContent.NPCType<DemonSpirit>();
    }

    public class DiscipleOfAttraidiesBanner : EnemyBanner
    {
        public override int PlaceStyle => 24;

        public override int NPCType => ModContent.NPCType<DiscipleOfAttraidies>();
    }

    public class DungeonMageBanner : EnemyBanner
    {
        public override int PlaceStyle => 25;

        public override int NPCType => ModContent.NPCType<DungeonMage>();
    }

    public class DunlendingBanner : EnemyBanner
    {
        public override int PlaceStyle => 26;

        public override int NPCType => ModContent.NPCType<Dunlending>();
    }

    public class DworcFleshhunterBanner : EnemyBanner
    {
        public override int PlaceStyle => 27;

        public override int NPCType => ModContent.NPCType<DworcFleshhunter>();
    }

    public class DworcVenomsniperBanner : EnemyBanner
    {
        public override int PlaceStyle => 28;

        public override int NPCType => ModContent.NPCType<DworcVenomsniper>();
    }

    public class DworcVoodoomasterBanner : EnemyBanner
    {
        public override int PlaceStyle => 29;

        public override int NPCType => ModContent.NPCType<DworcVoodoomaster>();
    }

    public class DworcVoodooShamanBanner : EnemyBanner
    {
        public override int PlaceStyle => 30;

        public override int NPCType => ModContent.NPCType<DworcVoodooShaman>();
    }

    public class FirebombHollowBanner : EnemyBanner
    {
        public override int PlaceStyle => 31;

        public override int NPCType => ModContent.NPCType<FirebombHollow>();
    }

    public class FlameBatBanner : EnemyBanner
    {
        public override int PlaceStyle => 32;

        public override int NPCType => ModContent.NPCType<FlameBat>();
    }

    public class GhostOfTheDarkmoonKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 33;

        public override int NPCType => ModContent.NPCType<GhostOfTheDarkmoonKnight>();
    }

    public class GhostOfTheForgottenKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 34;

        public override int NPCType => ModContent.NPCType<GhostoftheForgottenKnight>();
    }

    public class GhostOfTheForgottenWarriorBanner : EnemyBanner
    {
        public override int PlaceStyle => 35;

        public override int NPCType => ModContent.NPCType<GhostOfTheForgottenWarrior>();
    }

    public class FireLurkerBanner : EnemyBanner
    {
        public override int PlaceStyle => 36;

        public override int NPCType => ModContent.NPCType<FireLurker>();
    }

    public class JungleSentreeBanner : EnemyBanner
    {
        public override int PlaceStyle => 38;

        public override int NPCType => ModContent.NPCType<JungleSentree>();
    }

    public class ManHunterBanner : EnemyBanner
    {
        public override int PlaceStyle => 39;

        public override int NPCType => ModContent.NPCType<ManHunter>();
    }

    public class MarilithSpiritTwinBanner : EnemyBanner
    {
        public override int PlaceStyle => 40;

        public override int NPCType => ModContent.NPCType<MarilithSpiritTwin>();
    }

    public class MindflayerIllusionBanner : EnemyBanner
    {
        public override int PlaceStyle => 41;

        public override int NPCType => ModContent.NPCType<MindflayerIllusion>();
    }

    public class MindflayerKingServantBanner : EnemyBanner
    {
        public override int PlaceStyle => 42;

        public override int NPCType => ModContent.NPCType<MindflayerKingServant>();
    }

    public class MindflayerServantBanner : EnemyBanner
    {
        public override int PlaceStyle => 43;

        public override int NPCType => ModContent.NPCType<MindflayerServant>();
    }

    public class MutantToadBanner : EnemyBanner
    {
        public override int PlaceStyle => 44;

        public override int NPCType => ModContent.NPCType<MutantToad>();
    }

    public class NecromancerBanner : EnemyBanner
    {
        public override int PlaceStyle => 45;

        public override int NPCType => ModContent.NPCType<Necromancer>();
    }

    public class NecromancerElementalBanner : EnemyBanner
    {
        public override int PlaceStyle => 46;

        public override int NPCType => ModContent.NPCType<NecromancerElemental>();
    }

    public class ParaspriteBanner : EnemyBanner
    {
        public override int PlaceStyle => 47;

        public override int NPCType => ModContent.NPCType<Parasprite>();
    }

    public class QuaraHydromancerBanner : EnemyBanner
    {
        public override int PlaceStyle => 48;

        public override int NPCType => ModContent.NPCType<QuaraHydromancer>();
    }

    public class RedCloudHunterBanner : EnemyBanner
    {
        public override int PlaceStyle => 49;

        public override int NPCType => ModContent.NPCType<RedCloudHunter>();
    }

    public class HollowSoldierBanner : EnemyBanner
    {
        public override int PlaceStyle => 50;

        public override int NPCType => ModContent.NPCType<HollowSoldier>();
    }

    public class ShadowMageBanner : EnemyBanner
    {
        public override int PlaceStyle => 51;

        public override int NPCType => ModContent.NPCType<ShadowMage>();
    }

    public class SnowOwlBanner : EnemyBanner
    {
        public override int PlaceStyle => 52;

        public override int NPCType => ModContent.NPCType<SnowOwl>();
    }

    public class TibianAmazonBanner : EnemyBanner
    {
        public override int PlaceStyle => 53;

        public override int NPCType => ModContent.NPCType<TibianAmazon>();
    }

    public class TibianValkyrieBanner : EnemyBanner
    {
        public override int PlaceStyle => 54;

        public override int NPCType => ModContent.NPCType<TibianValkyrie>();
    }

    public class TonberryBanner : EnemyBanner
    {
        public override int PlaceStyle => 55;

        public override int NPCType => ModContent.NPCType<Tonberry>();
    }

    public class WarlockBanner : EnemyBanner
    {
        public override int PlaceStyle => 56;

        public override int NPCType => ModContent.NPCType<Warlock>();
    }

    public class WaterSpiritBanner : EnemyBanner
    {
        public override int PlaceStyle => 57;

        public override int NPCType => ModContent.NPCType<WaterSpirit>();
    }

    public class ParasyticWormBanner : EnemyBanner
    {
        public override int PlaceStyle => 58;

        public override int NPCType => ModContent.NPCType<ParasyticWormHead>();
    }

    public class JungleWyvernJuvenileBanner : EnemyBanner
    {
        public override int PlaceStyle => 59;

        public override int NPCType => ModContent.NPCType<NPCs.Enemies.JungleWyvernJuvenile.JungleWyvernJuvenileHead>();
    }

    public class SerpentOfTheAbyssBanner : EnemyBanner
    {
        public override int PlaceStyle => 60;

        public override int NPCType => ModContent.NPCType<NPCs.Enemies.SuperHardMode.SerpentOfTheAbyss.SerpentOfTheAbyssHead>();
    }

    public class AbysswalkerBanner : EnemyBanner
    {
        public override int PlaceStyle => 61;

        public override int NPCType => ModContent.NPCType<Abysswalker>();
    }

    public class AncientDemonOfTheAbyssBanner : EnemyBanner
    {
        public override int PlaceStyle => 62;

        public override int NPCType => ModContent.NPCType<AncientDemonOfTheAbyss>();
    }

    public class BarrowWightNemesisBanner : EnemyBanner
    {
        public override int PlaceStyle => 63;

        public override int NPCType => ModContent.NPCType<BarrowWightNemesis>();
    }

    public class BarrowWightPhantomBanner : EnemyBanner
    {
        public override int PlaceStyle => 64;

        public override int NPCType => ModContent.NPCType<BarrowWightPhantom>();
    }

    // Backwards compatibility with the previously inconsistently named banner.
    [LegacyName("BasiliskHunter")]
    public class BasiliskHunterBanner : EnemyBanner
    {
        public override int PlaceStyle => 65;

        public override int NPCType => ModContent.NPCType<BasiliskHunter>();
    }

    public class CorruptedElementalBanner : EnemyBanner
    {
        public override int PlaceStyle => 66;

        public override int NPCType => ModContent.NPCType<CorruptedElemental>();
    }

    public class CorruptedHornetBanner : EnemyBanner
    {
        public override int PlaceStyle => 67;

        public override int NPCType => ModContent.NPCType<CorruptedHornet>();
    }

    public class CrystalKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 68;

        public override int NPCType => ModContent.NPCType<CrystalKnight>();
    }

    public class DarkBloodKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 70;

        public override int NPCType => ModContent.NPCType<DarkBloodKnight>();
    }

    public class DarkKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 71;

        public override int NPCType => ModContent.NPCType<DarkKnight>();
    }

    public class HollowWarriorBanner : EnemyBanner
    {
        public override int PlaceStyle => 72;

        public override int NPCType => ModContent.NPCType<HollowWarrior>();
    }

    public class HydrisElementalBanner : EnemyBanner
    {
        public override int PlaceStyle => 73;

        public override int NPCType => ModContent.NPCType<HydrisElemental>();
    }

    public class HydrisNecromancerBanner : EnemyBanner
    {
        public override int PlaceStyle => 74;

        public override int NPCType => ModContent.NPCType<HydrisNecromancer>();
    }

    public class IceSkeletonBanner : EnemyBanner
    {
        public override int PlaceStyle => 75;

        public override int NPCType => ModContent.NPCType<IceSkeleton>();
    }

    public class ManOfWarBanner : EnemyBanner
    {
        public override int PlaceStyle => 76;

        public override int NPCType => ModContent.NPCType<ManOfWar>();
    }

    public class OolacileDemonBanner : EnemyBanner
    {
        public override int PlaceStyle => 77;

        public override int NPCType => ModContent.NPCType<OolacileDemon>();
    }

    public class OolacileKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 78;

        public override int NPCType => ModContent.NPCType<OolacileKnight>();
    }

    public class OolacileSorcererBanner : EnemyBanner
    {
        public override int PlaceStyle => 79;

        public override int NPCType => ModContent.NPCType<OolacileSorcerer>();
    }

    public class SlograIIBanner : EnemyBanner
    {
        public override int PlaceStyle => 80;

        public override int NPCType => ModContent.NPCType<SlograII>();
    }

    public class TaurusKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 81;

        public override int NPCType => ModContent.NPCType<TaurusKnight>();
    }

    public class TetsujinBanner : EnemyBanner
    {
        public override int PlaceStyle => 82;

        public override int NPCType => ModContent.NPCType<Tetsujin>();
    }

    public class VampireBatBanner : EnemyBanner
    {
        public override int PlaceStyle => 83;

        public override int NPCType => ModContent.NPCType<VampireBat>();
    }

    public class ArchdeaconBanner : EnemyBanner
    {
        public override int PlaceStyle => 84;

        public override int NPCType => ModContent.NPCType<Archdeacon>();
    }

    public class RedKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 85;

        public override int NPCType => ModContent.NPCType<RedKnight>();
    }

    public class GreatRedKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 86;

        public override int NPCType => ModContent.NPCType<GreatRedKnight>();
    }

    public class HumanityPhantomBanner : EnemyBanner
    {
        public override int PlaceStyle => 87;

        public override int NPCType => ModContent.NPCType<HumanityPhantom>();
    }

    public class LothricBlackKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 88;

        public override int NPCType => ModContent.NPCType<LothricBlackKnight>();
    }

    public class LothricKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 89;

        public override int NPCType => ModContent.NPCType<LothricKnight>();
    }

    public class LothricSpearKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 90;

        public override int NPCType => ModContent.NPCType<LothricSpearKnight>();
    }

    public class RingedKnightBanner : EnemyBanner
    {
        public override int PlaceStyle => 91;

        public override int NPCType => ModContent.NPCType<RingedKnight>();
    }
    #endregion
}
