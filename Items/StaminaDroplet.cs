using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class StaminaDroplet : ModItem
    {

        public override void SetDefaults()
        {
            item.width = 14;
            item.height = 20;
            item.rare = ItemRarityID.Green;
        }

        public override bool ItemSpace(Player player)
        {
            return true;
        }

        public override bool OnPickup(Player player)
        {

            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent += player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 / 2;

            if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2)
            {
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent = player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2;
            }

            for (int i = 0; i < 15; i++)
            {
                Dust.NewDust(player.position, player.height, player.width, 107, 0, 0, 100, default, Main.rand.NextFloat(.7f, 1.2f));
            }

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Microsoft.Xna.Framework.Color lightColor, Microsoft.Xna.Framework.Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = Main.itemTexture[item.type];
            spriteBatch.Draw(texture, item.Center - Main.screenPosition, new Rectangle(0, 0, texture.Width, texture.Height), Color.White, 0f, new Vector2(7, 9), item.scale, SpriteEffects.None, 0f);

            return false;
        }
    }
}
