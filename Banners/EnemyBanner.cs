using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace tsorcRevamp.Banners
{
    public abstract class EnemyBanner : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 10;
            Item.height = 24;
            Item.maxStack = 99;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.scale = 1.5f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(0, 0, 10, 0);
            Item.createTile = ModContent.TileType<EnemyBannerTile>();
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
            //DustType = -1;
            TileID.Sets.DisableSmartCursor[Type] = true;
            ModTranslation name = CreateMapEntryName();
            name.SetDefault("Banner");
            AddMapEntry(new Color(13, 88, 130), name);
        }


        #region KillMultiTile


        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            int style = frameX / 18;
            string item;
            switch (style)
            {
                case 0:
                    item = "GuardianCorruptorBanner";
                    break;
                case 1:
                    item = "CloudBatBanner";
                    break;
                case 2:
                    item = "ArmoredWraithBanner";
                    break;
                case 3:
                    item = "ObsidianJellyfishBanner";
                    break;
                case 4:
                    item = "StoneGolemBanner";
                    break;
                case 5:
                    item = "AbandonedStumpBanner";
                    break;
                case 6:
                    item = "ResentfulSeedlingBanner";
                    break;
                case 7:
                    item = "LivingShroomBanner";
                    break;
                case 8:
                    item = "LivingShroomThiefBanner";
                    break;
                case 9:
                    item = "LivingGlowshroomBanner";
                    break;
                case 10:
                    item = "AncientDemonBanner";
                    break;
                case 11:
                    item = "UndeadCasterBanner";
                    break;
                case 12:
                    item = "ChickenBanner";
                    break;
                case 13:
                    item = "AttraidiesIllusionBanner";
                    break;
                case 14:
                    item = "CosmicCrystalLizardBanner";
                    break;
                case 15:
                    item = "AssassinBanner";
                    break;
                case 16:
                    item = "AttraidiesManifestationBanner";
                    break;
                case 17:
                    item = "BarrowWightBanner";
                    break;
                case 18:
                    item = "BasiliskShifterBanner";
                    break;
                case 19:
                    item = "BasiliskWalkerBanner";
                    break;
                case 20:
                    item = "BlackKnightBanner";
                    break;
                case 21:
                    item = "CrazedDemonSpiritBanner";
                    break;
                case 22:
                    item = "DarkElfMageBanner";
                    break;
                case 23:
                    item = "DemonSpiritBanner";
                    break;
                case 24:
                    item = "DiscipleOfAttraidiesBanner";
                    break;
                case 25:
                    item = "DungeonMageBanner";
                    break;
                case 26:
                    item = "DunlendingBanner";
                    break;
                case 27:
                    item = "DworcFleshhunterBanner";
                    break;
                case 28:
                    item = "DworcVenomsniperBanner";
                    break;
                case 29:
                    item = "DworcVoodoomasterBanner";
                    break;
                case 30:
                    item = "DworcVoodooShamanBanner";
                    break;
                case 31:
                    item = "FirebombHollowBanner";
                    break;
                case 32:
                    item = "FlameBatBanner";
                    break;
                case 33:
                    item = "GhostOfTheDarkmoonKnightBanner";
                    break;
                case 34:
                    item = "GhostOfTheForgottenKnightBanner";
                    break;
                case 35:
                    item = "GhostOfTheForgottenWarriorBanner";
                    break;
                case 36:
                    item = "GreatRedKnightOfArtoriasBanner";
                    break;
                case 37:
                    item = "HeroOfLumeliaBanner";
                    break;
                case 38:
                    item = "JungleSentreeBanner";
                    break;
                case 39:
                    item = "ManHunterBanner";
                    break;
                case 40:
                    item = "MarilithSpiritTwinBanner";
                    break;
                case 41:
                    item = "MindflayerIllusionBanner";
                    break;
                case 42:
                    item = "MindflayerKingServantBanner";
                    break;
                case 43:
                    item = "MindflayerServantBanner";
                    break;
                case 44:
                    item = "MutantToadBanner";
                    break;
                case 45:
                    item = "NecromancerBanner";
                    break;
                case 46:
                    item = "NecromancerElementalBanner";
                    break;
                case 47:
                    item = "ParaspriteBanner";
                    break;
                case 48:
                    item = "QuaraHydromancerBanner";
                    break;
                case 49:
                    item = "RedCloudHunterBanner";
                    break;
                case 50:
                    item = "RedKnightofArtoriasBanner";
                    break;
                case 51:
                    item = "ShadowMageBanner";
                    break;
                case 52:
                    item = "SnowOwlBanner";
                    break;
                case 53:
                    item = "TibianAmazonBanner";
                    break;
                case 54:
                    item = "TibianValkyrieBanner";
                    break;
                case 55:
                    item = "TonberryBanner";
                    break;
                case 56:
                    item = "WarlockBanner";
                    break;
                case 57:
                    item = "WaterSpiritBanner"; //does this thing even spawn?
                    break;
                case 58:
                    item = "ParasyticWormBanner";
                    break;
                case 59:
                    item = "JungleWyvernJuvenileBanner";
                    break;


                case 60:
                    item = "SerpentOfTheAbyssBanner";
                    break;
                case 61:
                    item = "AbysswalkerBanner";
                    break;
                case 62:
                    item = "AncientDemonOfTheAbyssBanner";
                    break;
                case 63:
                    item = "BarrowWightNemesisBanner";
                    break;
                case 64:
                    item = "BarrowWightPhantomBanner";
                    break;
                case 65:
                    item = "BasiliskHunterBanner";
                    break;
                case 66:
                    item = "CorruptedElementalBanner";
                    break;
                case 67:
                    item = "CorruptedHornetBanner";
                    break;
                case 68:
                    item = "CrystalKnightBanner";
                    break;
                case 69:
                    item = "CrystalKnightIIBanner";
                    break;
                case 70:
                    item = "DarkBloodKnightBanner";
                    break;
                case 71:
                    item = "DarkKnightBanner";
                    break;
                case 72:
                    item = "GreatRedKnightOfTheAbyssBanner";
                    break;
                case 73:
                    item = "HydrisElementalBanner";
                    break;
                case 74:
                    item = "HydrisNecromancerBanner";
                    break;
                case 75:
                    item = "IceSkeletonBanner";
                    break;
                case 76:
                    item = "ManOfWarBanner";
                    break;
                case 77:
                    item = "OolacileDemonBanner";
                    break;
                case 78:
                    item = "OolacileKnightBanner";
                    break;
                case 79:
                    item = "OolacileSorcererBanner";
                    break;
                case 80:
                    item = "SlograIIBanner";
                    break;
                case 81:
                    item = "TaurusKnightBanner";
                    break;
                case 82:
                    item = "TetsujinBanner";
                    break;
                case 83:
                    item = "VampireBatBanner";
                    break;
                case 84:
                    item = "ArchdeaconBanner";
                    break;

                default:
                    return;
            }
            Item.NewItem(new EntitySource_Misc("¯\\_(ツ)_/¯"), i * 16, j * 16, 16, 48, Mod.Find<ModItem>(item).Type);
        }


        #endregion


        #region NearbyEffects


        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (closer)
            {
                Player player = Main.LocalPlayer;
                int style = Main.tile[i, j].TileFrameX / 18;
                string type;
                switch (style)
                {
                    case 0:
                        type = "GuardianCorruptor";
                        break;
                    case 1:
                        type = "CloudBat";
                        break;
                    case 2:
                        type = "ArmoredWraith";
                        break;
                    case 3:
                        type = "ObsidianJellyfish";
                        break;
                    case 4:
                        type = "StoneGolem";
                        break;
                    case 5:
                        type = "AbandonedStump";
                        break;
                    case 6:
                        type = "ResentfulSeedling";
                        break;
                    case 7:
                        type = "LivingShroom";
                        break;
                    case 8:
                        type = "LivingShroomThief";
                        break;
                    case 9:
                        type = "LivingGlowshroom";
                        break;
                    case 10:
                        type = "AncientDemon";
                        break;
                    case 11:
                        type = "UndeadCaster";
                        break;
                    case 12:
                        type = "Chicken";
                        break;
                    case 13:
                        type = "AttraidiesIllusion";
                        break;
                    case 14:
                        type = "CosmicCrystalLizard";
                        break;
                    case 15:
                        type = "Assassin";
                        break;
                    case 16:
                        type = "AttraidiesManifestation";
                        break;
                    case 17:
                        type = "BarrowWight";
                        break;
                    case 18:
                        type = "BasiliskShifter";
                        break;
                    case 19:
                        type = "BasiliskWalker";
                        break;
                    case 20:
                        type = "BlackKnight";
                        break;
                    case 21:
                        type = "CrazedDemonSpirit";
                        break;
                    case 22:
                        type = "DarkElfMage";
                        break;
                    case 23:
                        type = "DemonSpirit";
                        break;
                    case 24:
                        type = "DiscipleOfAttraidies";
                        break;
                    case 25:
                        type = "DungeonMage";
                        break;
                    case 26:
                        type = "Dunlending";
                        break;
                    case 27:
                        type = "DworcFleshhunter";
                        break;
                    case 28:
                        type = "DworcVenomsniper";
                        break;
                    case 29:
                        type = "DworcVoodoomaster";
                        break;
                    case 30:
                        type = "DworcVoodooShaman";
                        break;
                    case 31:
                        type = "FirebombHollow";
                        break;
                    case 32:
                        type = "FlameBat";
                        break;
                    case 33:
                        type = "GhostOfTheDarkmoonKnight";
                        break;
                    case 34:
                        type = "GhostoftheForgottenKnight";
                        break;
                    case 35:
                        type = "GhostOfTheForgottenWarrior";
                        break;
                    case 36:
                        type = "GreatRedKnightofArtorias";
                        break;
                    case 37:
                        type = "HeroofLumelia";
                        break;
                    case 38:
                        type = "JungleSentree";
                        break;
                    case 39:
                        type = "ManHunter";
                        break;
                    case 40:
                        type = "MarilithSpiritTwin";
                        break;
                    case 41:
                        type = "MindflayerIllusion";
                        break;
                    case 42:
                        type = "MindflayerKingServant";
                        break;
                    case 43:
                        type = "MindflayerServant";
                        break;
                    case 44:
                        type = "MutantToad";
                        break;
                    case 45:
                        type = "Necromancer";
                        break;
                    case 46:
                        type = "NecromancerElemental";
                        break;
                    case 47:
                        type = "Parasprite";
                        break;
                    case 48:
                        type = "QuaraHydromancer";
                        break;
                    case 49:
                        type = "RedCloudHunter";
                        break;
                    case 50:
                        type = "RedKnightofArtorias";
                        break;
                    case 51:
                        type = "ShadowMage";
                        break;
                    case 52:
                        type = "SnowOwl";
                        break;
                    case 53:
                        type = "TibianAmazon";
                        break;
                    case 54:
                        type = "TibianValkyrie";
                        break;
                    case 55:
                        type = "Tonberry";
                        break;
                    case 56:
                        type = "Warlock";
                        break;
                    case 57:
                        type = "WaterSpirit";
                        break;
                    case 58:
                        type = "ZombieWormHead";
                        break;
                    case 59:
                        type = "JungleWyvernJuvenileHead";
                        break;
                    case 60:
                        type = "SerpentOfTheAbyssHead";
                        break;
                    case 61:
                        type = "Abysswalker";
                        break;
                    case 62:
                        type = "AncientDemonOfTheAbyss";
                        break;
                    case 63:
                        type = "BarrowWightNemesis";
                        break;
                    case 64:
                        type = "BarrowWightPhantom";
                        break;
                    case 65:
                        type = "BasiliskHunter";
                        break;
                    case 66:
                        type = "CorruptedElemental";
                        break;
                    case 67:
                        type = "CorruptedHornet";
                        break;
                    case 68:
                        type = "CrystalKnight";
                        break;
                    case 69:
                        type = "CrystalKnightII";
                        break;
                    case 70:
                        type = "DarkBloodKnight";
                        break;
                    case 71:
                        type = "DarkKnight";
                        break;
                    case 72:
                        type = "GreatRedKnightOfTheAbyss";
                        break;
                    case 73:
                        type = "HydrisElemental";
                        break;
                    case 74:
                        type = "HydrisNecromancer";
                        break;
                    case 75:
                        type = "IceSkeleton";
                        break;
                    case 76:
                        type = "ManOfWar";
                        break;
                    case 77:
                        type = "OolacileDemon";
                        break;
                    case 78:
                        type = "OolacileKnight";
                        break;
                    case 79:
                        type = "OolacileSorcerer";
                        break;
                    case 80:
                        type = "SlograII";
                        break;
                    case 81:
                        type = "TaurusKnight";
                        break;
                    case 82:
                        type = "Tetsujin";
                        break;
                    case 83:
                        type = "VampireBat";
                        break;
                    case 84:
                        type = "Archdeacon";
                        break;

                    default:
                        return;
                }
                Main.SceneMetrics.NPCBannerBuff[Mod.Find<ModNPC>(type).Type] = true;
                Main.SceneMetrics.hasBanner = true;
            }
        }


        #endregion


    }


    #region Banner Classes


    #region Pre-SHM


    public class GuardianCorruptorBanner : EnemyBanner
    {
        //you may notice that all mod enemies with banners drop a guardian corruptor banner. this is NOT a bug
        //it chooses banner type by placeStyle, and they all have placeStyle 0, aka Guardian Corruptor
        //once they have a sprite and placeStyle gets updated, they'll drop the right banners
        //public override string Texture => "tsorcRevamp/Banners/placeholder"; //change name or remove line once texture added

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Guardian Corruptor");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 0; //change when texture added
        }
    }
    public class CloudBatBanner : EnemyBanner
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Cloud Bat");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 1;
        }
    }

    public class ArmoredWraithBanner : EnemyBanner
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Armored Wraith");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 2;
        }
    }

    public class ObsidianJellyfishBanner : EnemyBanner
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Obsidian Jellyfish");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 3;
        }
    }

    public class StoneGolemBanner : EnemyBanner
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Stone Golem");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 4;
        }
    }

    public class AbandonedStumpBanner : EnemyBanner
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Abandoned Stump");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 5;
        }
    }
    public class ResentfulSeedlingBanner : EnemyBanner
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Resentful Seedling");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 6;
        }
    }
    public class LivingShroomBanner : EnemyBanner
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fleeing Fungi Banner");
            Tooltip.SetDefault("Nearby players get a bonus against: Fleeing Fungi");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 7;
        }
    }
    public class LivingShroomThiefBanner : EnemyBanner
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fungi Felon Banner");
            Tooltip.SetDefault("Nearby players get a bonus against: Fungi Felon");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 8;
        }
    }
    public class LivingGlowshroomBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Living Glowshroom");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 9; //change when texture added
        }
    }
    public class AncientDemonBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Ancient Demon");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 10; //change when texture added
        }
    }
    public class UndeadCasterBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Undead Caster");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 11; //change when texture added
        }
    }
    public class ChickenBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Chicken"); //you're gonna need it
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 12; //change when texture added
        }
    }
    public class AttraidiesIllusionBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Attraidies Illusion");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 13; //change when texture added
        }
    }
    public class CosmicCrystalLizardBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Cosmic Crystal Lizard");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 14; //change when texture added
        }
    }
    public class AssassinBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Assassin");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 15; //change when texture added
        }
    }
    public class AttraidiesManifestationBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Attraidies Manifestation");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 16; //change when texture added
        }
    }
    public class BarrowWightBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Barrow Wight");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 17; //change when texture added
        }
    }
    public class BasiliskShifterBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Basilisk Shifter");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 18; //change when texture added
        }
    }
    public class BasiliskWalkerBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Basilisk Walker");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 19; //change when texture added
        }
    }
    public class BlackKnightBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Black Knight");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 20; //change when texture added
        }
    }
    public class CrazedDemonSpiritBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Crazed Demon Spirit");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 21; //change when texture added
        }
    }
    public class DarkElfMageBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Dark Elf Mage");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 22; //change when texture added
        }
    }
    public class DemonSpiritBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Demon Spirit");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 23; //change when texture added
        }
    }
    public class DiscipleOfAttraidiesBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Disciple of Attraidies");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 24; //change when texture added
        }
    }
    public class DungeonMageBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Dungeon Mage");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 25; //change when texture added
        }
    }
    public class DunlendingBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Dunlending");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 26; //change when texture added
        }
    }
    public class DworcFleshhunterBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Dworc Fleshhunter");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 27; //change when texture added
        }
    }
    public class DworcVenomsniperBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Dworc Venomsniper");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 28; //change when texture added
        }
    }
    public class DworcVoodoomasterBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Dworc Alchemist");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 29; //change when texture added
        }
    }
    public class DworcVoodooShamanBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Dworc Voodoo Shaman");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 30; //change when texture added
        }
    }
    public class FirebombHollowBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Firebomb Hollow");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 31; //change when texture added
        }
    }
    public class FlameBatBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Flame Bat");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 32; //change when texture added
        }
    }
    public class GhostOfTheDarkmoonKnightBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Ghost of The Darkmoon Knight");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 33; //change when texture added
        }
    }
    public class GhostOfTheForgottenKnightBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Ghost of The Forgotten Knight");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 34; //change when texture added
        }
    }
    public class GhostOfTheForgottenWarriorBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Ghost Of The Forgotten Warrior");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 35; //change when texture added
        }
    }
    public class GreatRedKnightOfArtoriasBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Great Red Knight of Artorias");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 36; //change when texture added
        }
    }
    public class HeroOfLumeliaBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Hero of Lumelia");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 37; //change when texture added
        }
    }
    public class JungleSentreeBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Jungle Sentree");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 38; //change when texture added
        }
    }
    public class ManHunterBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Man Hunter");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 39; //change when texture added
        }
    }
    public class MarilithSpiritTwinBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Marilith Spirit Twin");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 40; //change when texture added
        }
    }
    public class MindflayerIllusionBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Mindflayer Illusion");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 41; //change when texture added
        }
    }
    public class MindflayerKingServantBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Mindflayer King Servant");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 42; //change when texture added
        }
    }
    public class MindflayerServantBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Mindflayer Servant");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 43; //change when texture added
        }
    }
    public class MutantToadBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Mutant Toad");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 44; //change when texture added
        }
    }
    public class NecromancerBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Necromancer");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 45; //change when texture added
        }
    }
    public class NecromancerElementalBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Necromancer Elemental");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 46; //change when texture added
        }
    }
    public class ParaspriteBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Parasprite");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 47; //change when texture added
        }
    }
    public class QuaraHydromancerBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Quara Hydromancer");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 48; //change when texture added
        }
    }
    public class RedCloudHunterBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Red Cloud Hunter");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 49; //change when texture added
        }
    }
    public class RedKnightofArtoriasBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Red Knight of Artorias Banner");
            Tooltip.SetDefault("Nearby players get a bonus against: Red Knight of Artorias");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 50; //change when texture added
        }
    }
    public class ShadowMageBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Shadow Mage");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 51; //change when texture added
        }
    }
    public class SnowOwlBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Snow Owl");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 52; //change when texture added
        }
    }
    public class TibianAmazonBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Tibian Amazon");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 53; //change when texture added
        }
    }
    public class TibianValkyrieBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Tibian Valkyrie");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 54; //change when texture added
        }
    }
    public class TonberryBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Tonberry");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 55; //change when texture added
        }
    }
    public class WarlockBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Warlock");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 56; //change when texture added
        }
    }
    public class WaterSpiritBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Water Spirit");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 57; //change when texture added
        }
    }
    public class ParasyticWormBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Parasytic Worm");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 58; //change when texture added
        }
    }
    public class JungleWyvernJuvenileBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Jungle Wyvern Juvenile");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 59; //change when texture added
        }
    }


    #endregion


    #region SHM
    public class SerpentOfTheAbyssBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Serpent Of The Abyss");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 60; //change when texture added
        }
    }
    public class AbysswalkerBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Abysswalker");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 61; //change when texture added
        }
    }
    public class AncientDemonOfTheAbyssBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Ancient Demon Of The Abyss");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 62; //change when texture added
        }
    }
    public class BarrowWightNemesisBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Barrow Wight Nemesis");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 63; //change when texture added
        }
    }
    public class BarrowWightPhantomBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Barrow Wight Phantom");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 64; //change when texture added
        }
    }
    public class BasiliskHunter : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Basilisk Hunter");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 65; //change when texture added
        }
    }
    public class CorruptedElementalBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Corrupted Elemental");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 66; //change when texture added
        }
    }
    public class CorruptedHornetBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Corrupted Hornet");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 67; //change when texture added
        }
    }
    public class CrystalKnightBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Crystal Knight");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 68; //change when texture added
        }
    }
    public class DarkBloodKnightBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Dark Blood Knight");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 70; //change when texture added
        }
    }
    public class DarkKnightBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Dark Knight");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 71; //change when texture added
        }
    }
    public class GreatRedKnightOfTheAbyssBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Great Red Knight Of The Abyss");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 72; //change when texture added
        }
    }
    public class HydrisElementalBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Hydris Elemental");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 73; //change when texture added
        }
    }
    public class HydrisNecromancerBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Hydris Necromancer");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 74; //change when texture added
        }
    }
    public class IceSkeletonBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Ice Skeleton");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 75; //change when texture added
        }
    }
    public class ManOfWarBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Man Of War");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 76; //change when texture added
        }
    }
    public class OolacileDemonBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ephemeral Oolacile Demon Banner");
            Tooltip.SetDefault("Nearby players get a bonus against: Oolacile Demon");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 77; //change when texture added
        }
    }
    public class OolacileKnightBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Oolacile Knight");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 78; //change when texture added
        }
    }
    public class OolacileSorcererBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Oolacile Sorcerer");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 79; //change when texture added
        }
    }
    public class SlograIIBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Slogra Banner");
            Tooltip.SetDefault("Nearby players get a bonus against: Slogra");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 80; //change when texture added
        }
    }
    public class TaurusKnightBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Taurus Knight");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 81; //change when texture added
        }
    }
    public class TetsujinBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Tetsujin");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 82; //change when texture added
        }
    }
    public class VampireBatBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Vampire Bat");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 83; //change when texture added
        }
    }
    public class ArchdeaconBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Archdeacon");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.placeStyle = 84; //change when texture added
        }
    }

    #endregion


    #endregion

}
