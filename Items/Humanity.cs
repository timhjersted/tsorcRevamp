using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class Humanity : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently increases maximum life by 20" + 
                               "\nWon't increase max HP over the maximum achieved " +
                               "\nvia Life Crystals or Life Fruit");
        }

        public override void SetDefaults() {
            item.width = 16;
            item.height = 24;
            item.rare = ItemRarityID.Green;
            item.value = 75000;
            item.useAnimation = 15;
            item.useTime = 15;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.UseSound = SoundID.Item4;
            item.maxStack = 500;
            item.consumable = true;
        }

        public override bool CanUseItem(Player player) {
            return (player.statLifeMax < player.GetModPlayer<tsorcRevampPlayer>().MaxAcquiredHP);
        }

        public override bool UseItem(Player player) {

            player.statLifeMax += 20;
            player.statLife += 20; //BOTC can still heal from this, as you can in DS
            if (Main.myPlayer == player.whoAmI) {
                player.HealEffect(20, true);
            }
            if (player.statLifeMax > 500) {
                player.statLife = player.statLifeMax2;
                player.statLifeMax = 500;
            }
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;

            //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
            int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
            if (ttindex != -1)
            {// if we find one
             //insert the extra tooltip line
                tooltips.Insert(ttindex + 1, new TooltipLine(mod, "", $"Current max: { (player.GetModPlayer<tsorcRevampPlayer>().MaxAcquiredHP) }"));
            }
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Microsoft.Xna.Framework.Color lightColor, Microsoft.Xna.Framework.Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {

            Texture2D texture = Main.itemTexture[item.type];
            spriteBatch.Draw(texture, item.position - Main.screenPosition, new Rectangle(0, 0, texture.Width, texture.Height), Color.White, 0f, new Vector2(0, 4), item.scale, SpriteEffects.None, 0f);

            return false;
        }
    }
}
