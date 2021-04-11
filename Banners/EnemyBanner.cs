using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace tsorcRevamp.Banners {
    public abstract class EnemyBanner : ModItem {

        public override void SetDefaults() {
            item.width = 10;
            item.height = 24;
            item.maxStack = 99;
            item.useTurn = true;
            item.autoReuse = true;
            item.useAnimation = 15;
            item.useTime = 10;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.consumable = true;
            item.rare = ItemRarityID.Blue;
            item.value = Item.buyPrice(0, 0, 10, 0);
            item.createTile = ModContent.TileType<EnemyBannerTile>();
        }
    }


    public class EnemyBannerTile : ModTile {
        public override void SetDefaults() {
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
            dustType = -1;
            disableSmartCursor = true;
            ModTranslation name = CreateMapEntryName();
            name.SetDefault("Banner");
            AddMapEntry(new Color(13, 88, 130), name);
        }
        public override void KillMultiTile(int i, int j, int frameX, int frameY) {
            int style = frameX / 18;
            string item;
            switch (style) {
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
                    item = "CorruptJellyfishBanner";
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
                    item = "LivingShroomPoisonBanner";
                    break;
                case 10:
                    item = "LivingGlowshroomBanner";
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
                default:
                    return;
            }
            Item.NewItem(i * 16, j * 16, 16, 48, mod.ItemType(item));
        }

        public override void NearbyEffects(int i, int j, bool closer) {
            if (closer) {
                Player player = Main.LocalPlayer;
                int style = Main.tile[i, j].frameX / 18;
                string type;
                switch (style) {
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
                        type = "CorruptJellyfish";
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
                        type = "LivingShroomPoison";
                        break;
                    case 10:
                        type = "LivingGlowshroom";
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
                    default:
                        return;
                }
                player.NPCBannerBuff[mod.NPCType(type)] = true;
                player.hasBanner = true;
            }
        }

    }
    public class GuardianCorruptorBanner : EnemyBanner {
        //you may notice that all mod enemies with banners drop a guardian corruptor banner. this is NOT a bug
        //it chooses banner type by placeStyle, and they all have placeStyle 0, aka Guardian Corruptor
        //once they have a sprite and placeStyle gets updated, they'll drop the right banners
        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Nearby players get a bonus against: Guardian Corruptor");
        }
        public override void SetDefaults() {
            base.SetDefaults();
            item.placeStyle = 0; //change when texture added
        }
    }
    public class CloudBatBanner : EnemyBanner {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Nearby players get a bonus against: Cloud Bat");
        }
        public override void SetDefaults() {
            base.SetDefaults();
            item.placeStyle = 0; //change when texture added
        }
    }

    public class ArmoredWraithBanner : EnemyBanner {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Nearby players get a bonus against: Armored Wraith");
        }
        public override void SetDefaults() {
            base.SetDefaults();
            item.placeStyle = 0; //change when texture added
        }
    }

    public class CorruptJellyfishBanner : EnemyBanner {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Nearby players get a bonus against: Corrupt Jellyfish");
        }
        public override void SetDefaults() {
            base.SetDefaults();
            item.placeStyle = 0; //change when texture added
        }
    }

    public class StoneGolemBanner : EnemyBanner {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Nearby players get a bonus against: Stone Golem");
        }
        public override void SetDefaults() {
            base.SetDefaults();
            item.placeStyle = 0;
        }
    }

    public class AbandonedStumpBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Abandoned Stump");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            item.placeStyle = 0; //change when texture added
        }
    }
    public class ResentfulSeedlingBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Resentful Seedling");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            item.placeStyle = 0; //change when texture added
        }
    }
    public class LivingShroomBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Fleeing Fungi");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            item.placeStyle = 0; //change when texture added
        }
    }
    public class LivingShroomThiefBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Fungi Felon");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            item.placeStyle = 0; //change when texture added
        }
    }
    public class LivingShroomPoisonBanner : EnemyBanner
    {

        public override string Texture => "tsorcRevamp/Banners/placeholder";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Nearby players get a bonus against: Poisonous Living Shroom");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            item.placeStyle = 0; //change when texture added
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
            item.placeStyle = 0; //change when texture added
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
            item.placeStyle = 0; //change when texture added
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
            item.placeStyle = 0; //change when texture added
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
            item.placeStyle = 0; //change when texture added
        }
    }
}
