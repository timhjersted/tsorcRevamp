using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;

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
        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            int ttindex = tooltips.FindLastIndex(t => t.mod != null);
            if (ttindex != -1) {
                tooltips.Insert(ttindex + 1, new TooltipLine(mod, "", "Does not consume a buff slot."));
                tooltips.Insert(ttindex + 2, new TooltipLine(mod, "", "Use to toggle effect."));
            }
        }
    }
    public class PermanentObsidianSkinPotion: PermanentPotion {
        public override string Texture => "Terraria/Item_288";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Obsidian Skin buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[0] = !modPlayer.PermanentBuffToggles[0]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[0]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[0]) {
                player.lavaImmune = true;
                player.fireWalk = true;
                player.buffImmune[BuffID.OnFire] = true;
                player.buffImmune[BuffID.ObsidianSkin] = true;
            }
        }
    }

    public class PermanentRegenerationPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_289";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Regeneration buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[1] = !modPlayer.PermanentBuffToggles[1]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[1]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[1]) {
                player.lifeRegen += 4;
                player.buffImmune[BuffID.Regeneration] = true;
            }
        }
    }

    public class PermanentSwiftnessPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_290";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Swiftness buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[2] = !modPlayer.PermanentBuffToggles[2]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[2]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[2]) {
                player.moveSpeed += 0.25f;
                player.buffImmune[BuffID.Swiftness] = true;
            }
        }
    }
    public class PermanentGillsPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_291";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Gills buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[3] = !modPlayer.PermanentBuffToggles[3]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[3]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[3]) {
                player.gills = true;
                player.buffImmune[BuffID.Gills] = true;
            }
        }
    }
    public class PermanentIronskinPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_292";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Ironskin buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[4] = !modPlayer.PermanentBuffToggles[4]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[4]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[4]) {
                player.statDefense += 8;
                player.buffImmune[BuffID.Ironskin] = true;
            }
        }
    }
    public class PermanentManaRegenerationPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_293";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Mana Regeneration buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[5] = !modPlayer.PermanentBuffToggles[5]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[5]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[5]) {
                if (player.GetModPlayer<tsorcRevampPlayer>().manaShield == 0) {
                    player.manaRegenBuff = true;
                }
                player.buffImmune[BuffID.ManaRegeneration] = true;
            }
        }
    }
    public class PermanentMagicPowerPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_294";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Magic Power buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[6] = !modPlayer.PermanentBuffToggles[6]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[6]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[6]) {
                player.magicDamage += 0.2f;
                player.buffImmune[BuffID.MagicPower] = true;
            }
        }
    }
    public class PermanentFeatherfallPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_295";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Featherfall buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[7] = !modPlayer.PermanentBuffToggles[7]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[7]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[7]) {
                player.slowFall = true;
                player.buffImmune[BuffID.Featherfall] = true;
            }
        }
    }
    public class PermanentSpelunkerPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_296";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Spelunker buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[8] = !modPlayer.PermanentBuffToggles[8]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[8]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[8]) {
                player.findTreasure = true;
                player.buffImmune[BuffID.Spelunker] = true;
            }
        }
    }
    public class PermanentInvisibilityPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_297";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Invisibility buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[9] = !modPlayer.PermanentBuffToggles[9]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[9]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[9]) {
                player.invis = true;
                player.buffImmune[BuffID.Invisibility] = true;
            }
        }
    }
    public class PermanentShinePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_298";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Shine buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[10] = !modPlayer.PermanentBuffToggles[10]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[10]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[10]) {
                Lighting.AddLight((int)(player.Center.X / 16), (int)(player.Center.Y / 16), 0.8f, 0.95f, 1f);
                player.buffImmune[BuffID.Shine] = true;
            }
        }
    }
    public class PermanentNightOwlPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_299";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Night Owl buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[11] = !modPlayer.PermanentBuffToggles[11]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[11]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[11]) {
                player.nightVision = true;
                player.buffImmune[BuffID.NightOwl] = true;
            }
        }
    }
    public class PermanentBattlePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_300";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Battle buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[12] = !modPlayer.PermanentBuffToggles[12]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[12]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[12]) {
                player.enemySpawns = true;
                player.buffImmune[BuffID.Battle] = true;
            }
        }
    }
    public class PermanentThornsPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_301";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Thorns buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[13] = !modPlayer.PermanentBuffToggles[13]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[13]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[13]) {
                player.thorns += 1f;
                player.buffImmune[BuffID.Thorns] = true;
            }
        }
    }

    public class PermanentWaterWalkingPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_302";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Water Walking buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[14] = !modPlayer.PermanentBuffToggles[14]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[14]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[14]) {
                player.waterWalk = true;
                player.buffImmune[BuffID.WaterWalking] = true;
            }
        }
    }

    public class PermanentArcheryPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_303";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Archery buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[15] = !modPlayer.PermanentBuffToggles[15]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[15]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[15]) {
                player.archery = true;
                player.buffImmune[BuffID.Archery] = true;
            }
        }
    }
    public class PermanentHunterPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_304";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Hunter buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[16] = !modPlayer.PermanentBuffToggles[16]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[16]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[16]) {
                player.detectCreature = true;
                player.buffImmune[BuffID.Hunter] = true;
            }
        }
    }
    public class PermanentGravitationPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_305";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Gravitation buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[17] = !modPlayer.PermanentBuffToggles[17]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[17]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[17]) {
                player.gravControl = true;
                player.buffImmune[BuffID.Gravitation] = true;
            }
        }
    }
    public class PermanentAle : PermanentPotion {
        public override string Texture => "Terraria/Item_353";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Ale buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[18] = !modPlayer.PermanentBuffToggles[18]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[18]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[18]) {
                player.statDefense -= 4;
                player.meleeDamage += 0.1f;
                player.meleeCrit += 2;
                player.meleeSpeed += 0.1f;
                player.buffImmune[BuffID.Tipsy] = true;
            }
        }
    }

    public class PermanentFlaskOfVenom : PermanentPotion {
        public override string Texture => "Terraria/Item_1340";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Venom");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Venom buff. \nNot compatible with other weapon imbues.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[19] = !modPlayer.PermanentBuffToggles[19]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[19]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[19]) {
                player.meleeEnchant = 1;
                player.meleeDamage += 0.1f;
                player.buffImmune[BuffID.WeaponImbueVenom] = true;
            }
        }
    }
    public class PermanentFlaskOfCursedFlames : PermanentPotion {
        public override string Texture => "Terraria/Item_1353";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Cursed Flames");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Cursed Flames buff. \nNot compatible with other weapon imbues.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[20] = !modPlayer.PermanentBuffToggles[20]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[20]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[20]) {
                player.meleeEnchant = 2;
                player.meleeDamage += 0.1f;
                player.buffImmune[BuffID.WeaponImbueCursedFlames] = true;
            }
        }
    }

    public class PermanentFlaskOfFire : PermanentPotion {
        public override string Texture => "Terraria/Item_1354";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Fire");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Fire buff. \nNot compatible with other weapon imbues.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[21] = !modPlayer.PermanentBuffToggles[21]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[21]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[21]) {
                player.meleeEnchant = 3;
                player.meleeDamage += 0.1f;
                player.buffImmune[BuffID.WeaponImbueFire] = true;
            }
        }
    }

    public class PermanentFlaskOfGold : PermanentPotion {
        public override string Texture => "Terraria/Item_1355";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Gold");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Gold buff. \nNot compatible with other weapon imbues.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[22] = !modPlayer.PermanentBuffToggles[22]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[22]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[22]) {
                player.meleeEnchant = 4;
                player.meleeDamage += 0.1f;
                player.buffImmune[BuffID.WeaponImbueGold] = true;
            }
        }
    }

    public class PermanentFlaskOfIchor : PermanentPotion {
        public override string Texture => "Terraria/Item_1356";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Ichor");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Ichor buff. \nNot compatible with other weapon imbues.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[23] = !modPlayer.PermanentBuffToggles[23]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[23]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[23]) {
                player.meleeEnchant = 5;
                player.meleeDamage += 0.1f;
                player.buffImmune[BuffID.WeaponImbueIchor] = true;
            }
        }
    }

    public class PermanentFlaskOfNanites : PermanentPotion {
        public override string Texture => "Terraria/Item_1357";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Nanites");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Nanites buff. \nNot compatible with other weapon imbues.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[24] = !modPlayer.PermanentBuffToggles[24]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[24]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[24]) {
                player.meleeEnchant = 6;
                player.meleeDamage += 0.1f;
                player.buffImmune[BuffID.WeaponImbueNanites] = true;
            }
        }
    }

    public class PermanentFlaskOfParty : PermanentPotion {
        public override string Texture => "Terraria/Item_1358";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Party");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Confetti buff. \nNot compatible with other weapon imbues.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[25] = !modPlayer.PermanentBuffToggles[25]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[25]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[25]) {
                player.meleeEnchant = 7;
                player.meleeDamage += 0.1f;
                player.buffImmune[BuffID.WeaponImbueConfetti] = true;
            }
        }
    }

    public class PermanentFlaskOfPoison : PermanentPotion {
        public override string Texture => "Terraria/Item_1359";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Permanent Flask of Poison");
            Tooltip.SetDefault("Permanently grants the Weapon Imbue: Poison buff. \nNot compatible with other weapon imbues.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[26] = !modPlayer.PermanentBuffToggles[26]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[26]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[26]) {
                player.meleeEnchant = 8;
                player.meleeDamage += 0.1f;
                player.buffImmune[BuffID.WeaponImbuePoison] = true;
            }
        }
    }

    public class PermanentMiningPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2322";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Mining buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[27] = !modPlayer.PermanentBuffToggles[27]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[27]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[27]) {
                player.pickSpeed -= 0.25f;
                player.buffImmune[BuffID.Mining] = true;
            }
        }
    }

    public class PermanentHeartreachPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2323";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Heartreach buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[28] = !modPlayer.PermanentBuffToggles[28]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[28]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[28]) {
                player.lifeMagnet = true;
                player.buffImmune[BuffID.Heartreach] = true;
            }
        }
    }

    public class PermanentCalmingPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2324";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Calm buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[29] = !modPlayer.PermanentBuffToggles[29]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[29]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[29]) {
                player.calmed = true;
                player.buffImmune[BuffID.Calm] = true;
            }
        }
    }
    public class PermanentBuilderPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2325";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Builder buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[30] = !modPlayer.PermanentBuffToggles[30]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[30]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[30]) {
                player.tileSpeed += 0.25f;
                player.wallSpeed += 0.25f;
                player.blockRange++;
                player.buffImmune[BuffID.Builder] = true;
            }
        }
    }
    public class PermanentTitanPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2326";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Titan buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[31] = !modPlayer.PermanentBuffToggles[31]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[31]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[31]) {
                player.kbBuff = true;
                player.buffImmune[BuffID.Titan] = true;
            }
        }
    }

    public class PermanentFlipperPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2327";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Flipper buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[32] = !modPlayer.PermanentBuffToggles[32]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[32]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[32]) {
                player.accFlipper = true;
                player.ignoreWater = true;
                player.buffImmune[BuffID.Flipper] = true;
            }
        }
    }

    public class PermanentSummoningPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2328";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Summoning buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[33] = !modPlayer.PermanentBuffToggles[33]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[33]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[33]) {
                player.maxMinions++;
                player.buffImmune[BuffID.Summoning] = true;
            }
        }
    }
    public class PermanentDangersensePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2329";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Dangersense buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[34] = !modPlayer.PermanentBuffToggles[34]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[34]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[34]) {
                player.dangerSense = true;
                player.buffImmune[BuffID.Dangersense] = true;
            }
        }
    }

    public class PermanentAmmoReservationPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2344";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Ammo Reservation buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[35] = !modPlayer.PermanentBuffToggles[35]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[35]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[35]) {
                player.ammoPotion = true;
                player.buffImmune[BuffID.AmmoReservation] = true;
            }
        }
    }

    public class PermanentLifeforcePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2345";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Lifeforce buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[36] = !modPlayer.PermanentBuffToggles[36]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[36]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[36]) {
                player.lifeForce = true;
                player.statLifeMax2 += player.statLifeMax / 5 / 20 * 20; //why is this written like this? i will never understand vanilla terraria
                player.buffImmune[BuffID.Lifeforce] = true;
            }
        }
    }

    public class PermanentEndurancePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2346";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Endurance buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[37] = !modPlayer.PermanentBuffToggles[37]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[37]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[37]) {
                player.endurance += 0.1f;
                player.buffImmune[BuffID.Endurance] = true;
            }
        }
    }

    public class PermanentRagePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2347";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Rage buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[38] = !modPlayer.PermanentBuffToggles[38]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[38]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[38]) {
                player.magicCrit += 10;
                player.meleeCrit += 10;
                player.rangedCrit += 10;
                player.thrownCrit += 10;
                player.buffImmune[BuffID.Rage] = true;
            }
        }
    }

    public class PermanentInfernoPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2348";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Inferno buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[39] = !modPlayer.PermanentBuffToggles[39]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[39]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[39]) {
                player.buffImmune[BuffID.Inferno] = true;
                player.inferno = true;
                Lighting.AddLight((int)(player.Center.X / 16f), (int)(player.Center.Y / 16f), 0.65f, 0.4f, 0.1f);
                int num = 24;
                float num12 = 200f;
                bool flag = player.infernoCounter % 60 == 0;
                int damage = 10;
                if (player.whoAmI == Main.myPlayer) {
                    for (int l = 0; l < 200; l++) {
                        NPC nPC = Main.npc[l];
                        if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[num] && Vector2.Distance(player.Center, nPC.Center) <= num12) {
                            if (nPC.FindBuffIndex(num) == -1) {
                                nPC.AddBuff(num, 120);
                            }
                            if (flag) {
                                player.ApplyDamageToNPC(nPC, damage, 0f, 0, crit: false);
                            }
                        }
                    }
                    if (Main.netMode != NetmodeID.SinglePlayer && player.hostile) {
                        for (int m = 0; m < 255; m++) {
                            Player otherPlayer = Main.player[m];
                            if (otherPlayer != player && otherPlayer.active && !otherPlayer.dead && otherPlayer.hostile && !otherPlayer.buffImmune[24] && (otherPlayer.team != player.team || otherPlayer.team == 0) && Vector2.DistanceSquared(player.Center, otherPlayer.Center) <= num) {
                                if (otherPlayer.FindBuffIndex(num) == -1) {
                                    otherPlayer.AddBuff(num, 120);
                                }
                                if (flag) {
                                    otherPlayer.Hurt(PlayerDeathReason.LegacyEmpty(), damage, 0, pvp: true);
                                    if (Main.netMode != NetmodeID.SinglePlayer) {
                                        PlayerDeathReason reason = PlayerDeathReason.ByPlayer(otherPlayer.whoAmI);
                                        NetMessage.SendPlayerHurt(m, reason, damage, 0, critical: false, pvp: true, 0);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class PermanentWrathPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2349";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Wrath buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[40] = !modPlayer.PermanentBuffToggles[40]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[40]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[40]) {
                player.allDamage += 0.1f;
                player.buffImmune[BuffID.Wrath] = true;
            }
        }
    }

    public class PermanentFishingPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2354";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Fishing buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[41] = !modPlayer.PermanentBuffToggles[41]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[41]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[41]) {
                player.fishingSkill += 15;
                player.buffImmune[BuffID.Fishing] = true;
            }
        }
    }

    public class PermanentSonarPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2355";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Sonar buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[42] = !modPlayer.PermanentBuffToggles[42]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[42]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[42]) {
                player.sonarPotion = true;
                player.buffImmune[BuffID.Sonar] = true;
            }
        }
    }

    public class PermanentCratePotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2356";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Crate buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[43] = !modPlayer.PermanentBuffToggles[43]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[43]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[43]) {
                player.cratePotion = true;
                player.buffImmune[BuffID.Crate] = true;
            }
        }
    }

    public class PermanentWarmthPotion : PermanentPotion {
        public override string Texture => "Terraria/Item_2359";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Warmth buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[44] = !modPlayer.PermanentBuffToggles[44]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[44]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[44]) {
                player.resistCold = true;
                player.buffImmune[BuffID.Warmth] = true;
            }
        }
    }

    public class PermanentArmorDrug : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/ArmorDrugPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Armor Drug buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[49] = true;
            modPlayer.PermanentBuffToggles[51] = true;
            modPlayer.PermanentBuffToggles[46] = true;
            modPlayer.PermanentBuffToggles[45] = !modPlayer.PermanentBuffToggles[45]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[45]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (!modPlayer.PermanentBuffToggles[45]) {
                modPlayer.PermanentBuffToggles[49] = true;
                modPlayer.PermanentBuffToggles[51] = true;
                modPlayer.PermanentBuffToggles[46] = true;
                player.statDefense += 13;
                player.buffImmune[ModContent.BuffType<ArmorDrug>()] = true;
                player.buffImmune[ModContent.BuffType<DemonDrug>()] = true;
                player.buffImmune[ModContent.BuffType<Strength>()] = true;
                player.buffImmune[ModContent.BuffType<Battlefront>()] = true;
            }
        }
    }

    public class PermanentBattlefrontPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/BattlefrontPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Battlefront buff.");
        }

        
        public override bool UseItem(Player player) {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[49] = true;
            modPlayer.PermanentBuffToggles[51] = true;
            modPlayer.PermanentBuffToggles[45] = true;
            modPlayer.PermanentBuffToggles[46] = !modPlayer.PermanentBuffToggles[46]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[46]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            
            if (!modPlayer.PermanentBuffToggles[46]) {
                modPlayer.PermanentBuffToggles[49] = true;
                modPlayer.PermanentBuffToggles[51] = true;
                modPlayer.PermanentBuffToggles[45] = true;
                player.statDefense += 17;
                player.allDamage += 0.3f;
                player.magicCrit += 6;
                player.meleeCrit += 6;
                player.rangedCrit += 6;
                player.meleeSpeed += 0.2f;
                player.pickSpeed += 0.2f;
                player.thorns += 2f;
                player.enemySpawns = true;
                player.buffImmune[ModContent.BuffType<ArmorDrug>()] = true;
                player.buffImmune[ModContent.BuffType<DemonDrug>()] = true;
                player.buffImmune[ModContent.BuffType<Strength>()] = true;
                player.buffImmune[ModContent.BuffType<Battlefront>()] = true;
            }
        }
    }

    public class PermanentBoostPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/BoostPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Boost buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[47] = !modPlayer.PermanentBuffToggles[47]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[47]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[47]) {
                player.magicCrit += 5;
                player.meleeCrit += 5;
                player.rangedCrit += 5;
                player.buffImmune[ModContent.BuffType<Boost>()] = true;
            }
        }
    }

    public class PermanentCrimsonPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/CrimsonPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Crimson Drain buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[48] = !modPlayer.PermanentBuffToggles[48]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[48]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[48]) {
                player.GetModPlayer<tsorcRevampPlayer>().CrimsonDrain = true;
                player.buffImmune[ModContent.BuffType<CrimsonDrain>()] = true;
            }
        }
    }

    public class PermanentDemonDrug : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/DemonDrugPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Demon Drug buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>(); 
            modPlayer.PermanentBuffToggles[46] = true;
            modPlayer.PermanentBuffToggles[51] = true;
            modPlayer.PermanentBuffToggles[45] = true;
            modPlayer.PermanentBuffToggles[49] = !modPlayer.PermanentBuffToggles[49]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[49]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (!modPlayer.PermanentBuffToggles[49]) {
                modPlayer.PermanentBuffToggles[46] = true;
                modPlayer.PermanentBuffToggles[51] = true;
                modPlayer.PermanentBuffToggles[45] = true;
                player.allDamage += 0.2f;
                player.buffImmune[ModContent.BuffType<ArmorDrug>()] = true;
                player.buffImmune[ModContent.BuffType<DemonDrug>()] = true;
                player.buffImmune[ModContent.BuffType<Strength>()] = true;
                player.buffImmune[ModContent.BuffType<Battlefront>()] = true;
            }
        }
    }

    public class PermanentShockwavePotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/ShockwavePotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Shockwave buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[50] = !modPlayer.PermanentBuffToggles[50]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[50]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[50]) {
                player.GetModPlayer<tsorcRevampPlayer>().Shockwave = true;
                player.buffImmune[ModContent.BuffType<Shockwave>()] = true;
            }
        }
    }

    public class PermanentStrengthPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/StrengthPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Strength buff.");
        }

        
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[46] = true;
            modPlayer.PermanentBuffToggles[49] = true;
            modPlayer.PermanentBuffToggles[45] = true;
            modPlayer.PermanentBuffToggles[51] = !modPlayer.PermanentBuffToggles[51]; //toggle
            return true;            
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[51]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (!modPlayer.PermanentBuffToggles[51])
            {
                modPlayer.PermanentBuffToggles[46] = true;
                modPlayer.PermanentBuffToggles[49] = true;
                modPlayer.PermanentBuffToggles[45] = true;
                player.statDefense += 15;
                player.allDamage += 0.15f;
                player.meleeSpeed += 0.15f;
                player.pickSpeed += 0.15f;
                player.magicCrit += 2;
                player.meleeCrit += 2;
                player.rangedCrit += 2;
                player.buffImmune[ModContent.BuffType<ArmorDrug>()] = true;
                player.buffImmune[ModContent.BuffType<DemonDrug>()] = true;
                player.buffImmune[ModContent.BuffType<Strength>()] = true;
                player.buffImmune[ModContent.BuffType<Battlefront>()] = true;
            }
        }
    }

    public class PermanentSoulSiphonPotion : PermanentPotion {
        public override string Texture => "tsorcRevamp/Items/Potions/SoulSiphonPotion";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Soul Siphon buff.");

            ItemID.Sets.ItemIconPulse[item.type] = true; // Makes item pulsate in world.

        }


        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[52] = !modPlayer.PermanentBuffToggles[52]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[52]) {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++) {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player) {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.PermanentBuffToggles[52]) {
                modPlayer.SoulSiphon = true;
                modPlayer.SoulReaper += 5;
                modPlayer.ConsSoulChanceMult += 10;
                player.buffImmune[ModContent.BuffType<SoulSiphon>()] = true;
            }
        }
    }

    public class PermanentSoup : PermanentPotion
    {
        public override string Texture => "Terraria/Item_357";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Never go hungry again.");
        }


        public override bool UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[53] = !modPlayer.PermanentBuffToggles[53]; //toggle
            return true;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (!Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[53])
            {
                Texture2D texture = Main.itemTexture[item.type];
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                    spriteBatch.Draw(texture, position + offsetPositon, null, Main.DiscoColor, 0, origin, scale, SpriteEffects.None, 0f);
                }
            }
            return true;
        }

        public override void UpdateInventory(Player player)
        {
            if (!player.GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[53])
            {
                player.wellFed = true;
                player.statDefense += 2;
                player.meleeCrit += 2;
                player.rangedCrit += 2;
                player.magicCrit += 2;
                player.meleeSpeed += 0.05f;
                player.allDamage += 0.05f;
                player.minionKB += 0.5f;
                player.moveSpeed += 0.20f;

                player.buffImmune[BuffID.WellFed] = true;
            }
        }
    }

}
