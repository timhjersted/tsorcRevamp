using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions.PermanentPotions {
        //memory management is scary
    public abstract class PermanentPotion : ModItem {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetDefaults() {
            item.width = 16;
            item.height = 25;
            item.consumable = false;
            item.maxStack = 1;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 8;
            item.useTime = 8;
            item.UseSound = SoundID.Item21;
            item.rare = ItemRarityID.Orange;
        }
    }
    public class PermanentObsidianSkinPotion: PermanentPotion {
        public override string Texture => "Terraria/Item_288";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Obsidian Skin buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[0] = !modPlayer.PermanentBuffToggles[0]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[0]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentRegenerationPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_289";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Regeneration buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[1] = !modPlayer.PermanentBuffToggles[1]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[1]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentSwiftnessPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_290";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Swiftness buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[2] = !modPlayer.PermanentBuffToggles[2]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[2]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentGillsPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_291";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Gills buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[3] = !modPlayer.PermanentBuffToggles[3]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[3]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentIronskinPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_292";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Ironskin buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[4] = !modPlayer.PermanentBuffToggles[4]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[4]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentManaRegenerationPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_293";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Mana Regeneration buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[5] = !modPlayer.PermanentBuffToggles[5]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[5]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentMagicPowerPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_294";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Magic Power buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[6] = !modPlayer.PermanentBuffToggles[6]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[6]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentFeatherfallPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_295";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Featherfall buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[7] = !modPlayer.PermanentBuffToggles[7]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[7]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentSpelunkerPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_296";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Spelunker buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[8] = !modPlayer.PermanentBuffToggles[8]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[8]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentInvisibilityPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_297";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Invisibility buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[9] = !modPlayer.PermanentBuffToggles[9]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[9]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentShinePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_298";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Shine buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[10] = !modPlayer.PermanentBuffToggles[10]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[10]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentNightOwlPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_299";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Night Owl buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[11] = !modPlayer.PermanentBuffToggles[11]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[11]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentBattlePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_300";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Battle buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[12] = !modPlayer.PermanentBuffToggles[12]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[12]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentThornsPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_301";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Thorns buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[13] = !modPlayer.PermanentBuffToggles[13]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[13]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentWaterWalkingPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_302";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Water Walking buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[14] = !modPlayer.PermanentBuffToggles[14]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[14]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentArcheryPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_303";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Archery buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[15] = !modPlayer.PermanentBuffToggles[15]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[15]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentHunterPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_304";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Hunter buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[16] = !modPlayer.PermanentBuffToggles[16]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[16]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentGravitationPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_305";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Gravitation buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[17] = !modPlayer.PermanentBuffToggles[17]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[17]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentAle : PermanentPotion {
        public override string Texture => "Terraria/Item_353";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Ale buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[18] = !modPlayer.PermanentBuffToggles[18]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[18]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentFlaskOfVenom : PermanentPotion {
        public override string Texture => "Terraria/Item_1340";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Venom");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Venom buff. \nNot compatible with other flasks. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[19] = !modPlayer.PermanentBuffToggles[19]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[19]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentFlaskOfCursedFlames : PermanentPotion {
        public override string Texture => "Terraria/Item_1353";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Cursed Flames");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Cursed Flames buff. \nNot compatible with other flasks. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[20] = !modPlayer.PermanentBuffToggles[20]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[20]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentFlaskOfFire : PermanentPotion {
        public override string Texture => "Terraria/Item_1354";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Fire");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Fire buff. \nNot compatible with other flasks. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[21] = !modPlayer.PermanentBuffToggles[21]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[21]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentFlaskOfGold : PermanentPotion {
        public override string Texture => "Terraria/Item_1355";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Gold");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Gold buff. \nNot compatible with other flasks. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[22] = !modPlayer.PermanentBuffToggles[22]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[22]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentFlaskOfIchor : PermanentPotion {
        public override string Texture => "Terraria/Item_1356";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Ichor");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Ichor buff. \nNot compatible with other flasks. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[23] = !modPlayer.PermanentBuffToggles[23]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[23]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentFlaskOfNanites : PermanentPotion {
        public override string Texture => "Terraria/Item_1357";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Nanites");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Nanites buff. \nNot compatible with other flasks. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[24] = !modPlayer.PermanentBuffToggles[24]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[24]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentFlaskOfParty : PermanentPotion {
        public override string Texture => "Terraria/Item_1358";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Party");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Confetti buff. \nNot compatible with other flasks. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[25] = !modPlayer.PermanentBuffToggles[25]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[25]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentFlaskOfPoison : PermanentPotion {
        public override string Texture => "Terraria/Item_1359";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Poison");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Poison buff. \nNot compatible with other flasks. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[26] = !modPlayer.PermanentBuffToggles[26]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[26]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentMiningPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2322";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Mining buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[27] = !modPlayer.PermanentBuffToggles[27]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[27]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentHeartreachPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2323";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Heartreach buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[28] = !modPlayer.PermanentBuffToggles[28]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[28]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentCalmingPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2324";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Calm buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[29] = !modPlayer.PermanentBuffToggles[29]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[29]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentBuilderPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2325";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Builder buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[30] = !modPlayer.PermanentBuffToggles[30]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[30]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentTitanPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2326";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Titan buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[31] = !modPlayer.PermanentBuffToggles[31]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[31]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentFlipperPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2327";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Flipper buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[32] = !modPlayer.PermanentBuffToggles[32]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[32]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentSummoningPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2328";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Summoning buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[33] = !modPlayer.PermanentBuffToggles[33]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[33]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
    public class PermanentDangersensePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2329";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Dangersense buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[34] = !modPlayer.PermanentBuffToggles[34]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[34]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentAmmoReservationPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2344";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Ammo Reservation buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[35] = !modPlayer.PermanentBuffToggles[35]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[35]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentLifeforcePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2345";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Lifeforce buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[36] = !modPlayer.PermanentBuffToggles[36]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[36]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentEndurancePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2346";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Endurance buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[37] = !modPlayer.PermanentBuffToggles[37]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[37]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentRagePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2347";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Rage buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[38] = !modPlayer.PermanentBuffToggles[38]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[38]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentInfernoPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2348";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Inferno buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[39] = !modPlayer.PermanentBuffToggles[39]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[39]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentWrathPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2349";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Wrath buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[40] = !modPlayer.PermanentBuffToggles[40]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[40]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentFishingPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2354";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Fishing buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[41] = !modPlayer.PermanentBuffToggles[41]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[41]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentSonarPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2355";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Sonar buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[42] = !modPlayer.PermanentBuffToggles[42]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[42]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentCratePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2356";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the 'Crate chance increased' buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[43] = !modPlayer.PermanentBuffToggles[43]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[43]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentWarmthPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2359";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Warmth buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[44] = !modPlayer.PermanentBuffToggles[44]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[44]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentArmorDrugPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/ArmorDrugPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Armor Drug buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[45] = !modPlayer.PermanentBuffToggles[45]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[45]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentBattlefrontPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/BattlefrontPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Battlefront buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[46] = !modPlayer.PermanentBuffToggles[46]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[46]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentBoostPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/BoostPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Boost buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[47] = !modPlayer.PermanentBuffToggles[47]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[47]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentCrimsonPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/CrimsonPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Crimson Drain buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[48] = !modPlayer.PermanentBuffToggles[48]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[48]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentDemonDrugPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/DemonDrugPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Demon Drug buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[49] = !modPlayer.PermanentBuffToggles[49]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[49]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentShockwavePotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/ShockwavePotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Shockwave buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[50] = !modPlayer.PermanentBuffToggles[50]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[50]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentStrengthPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/StrengthPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Strength buff. \nUse to toggle effect.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[51] = !modPlayer.PermanentBuffToggles[51]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[51]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }

    public class PermanentSoulSiphonPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/SoulSiphonPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Soul Siphon buff." +
                "\nUse to toggle effect.");

            ItemID.Sets.ItemIconPulse[item.type] = true; // Makes item pulsate in world.

        }


        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[52] = !modPlayer.PermanentBuffToggles[52]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[52]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }
    }
}
