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
            Item.width = 14;
            Item.height = 20;
            Item.rare = ItemRarityID.Green;
            Item.value = 1; //prevents auto-trash
        }

        public override bool ItemSpace(Player player)
        {
            return true;
        }

        public override bool OnPickup(Player player)
        {
            if (player.whoAmI == Main.myPlayer) /*i dont know if this is necessary but better safe than sorry*/
            {
                tsorcRevampStaminaPlayer stamPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                float amount = stamPlayer.staminaResourceMax2 / 2;

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Drip with { Volume = 0.3f });


                if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    amount /= 2;
                }

                stamPlayer.staminaResourceCurrent += amount;

                if (stamPlayer.staminaResourceCurrent > stamPlayer.staminaResourceMax2)
                {
                    stamPlayer.staminaResourceCurrent = stamPlayer.staminaResourceMax2;
                }
                CombatText.NewText(player.getRect(), Color.YellowGreen, (int)amount);
                for (int i = 0; i < 15; i++)
                {
                    Dust.NewDust(player.position, player.height, player.width, 107, 0, 0, 100, default, Main.rand.NextFloat(.7f, 1.2f));
                }
            }

            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Microsoft.Xna.Framework.Color lightColor, Microsoft.Xna.Framework.Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            spriteBatch.Draw(texture, Item.position - Main.screenPosition, new Rectangle(0, 0, texture.Width, texture.Height), Color.White, 0f, new Vector2(2, 0), Item.scale, SpriteEffects.None, 0);

            return false;
        }
        public override bool GrabStyle(Player player)
        {
            Vector2 vectorItemToPlayer = player.Center - Item.Center;
            Vector2 movement = vectorItemToPlayer.SafeNormalize(default) * 10f;
            Item.velocity = movement;
            return true;
        }

        public override void GrabRange(Player player, ref int grabRange)
        {
            grabRange *= (1 + Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().StaminaReaper);
        }
    }
}
