using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions.PermanentPotions {
    class PermanentObsidianSkin : ModItem {

        public override string Texture => "Terraria/Item_288";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently grants the Obsidian Skin buff. \nUse to toggle effect.");
        }
        public override void SetDefaults() {
            item.width = 16;
            item.height = 25;
            item.consumable = false;
            item.maxStack = 360;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 15;
            item.useTime = 15;
            item.UseSound = SoundID.Item21;
            item.value = 2000;
            item.rare = ItemRarityID.Orange;
        }
        public override bool UseItem(Player player) {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.PermanentBuffToggles[0] = !modPlayer.PermanentBuffToggles[0]; //toggle
            return true;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            var enabled = Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().PermanentBuffToggles[0];
            if (enabled) {
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
