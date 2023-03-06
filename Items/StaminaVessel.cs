using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class StaminaVessel : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Permanently increases max stamina by 5%" +
                               "\nStamina maxes out after consuming 10"); */
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 24;
            Item.rare = ItemRarityID.Expert;
            Item.value = 0;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item4;
            Item.maxStack = 99;
            Item.consumable = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;

            tooltips.Insert(4, new TooltipLine(Mod, "", $"Consumed so far: {(player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax - 100) / 5}"));

        }

        public override bool CanUseItem(Player player)
        {
            return (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax < 150);
        }

        public override bool? UseItem(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax += 5;
            return true;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(Item.Center, 0.15f, 0.25f, 0.15f);

            if (Main.rand.NextBool(20))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X, Item.position.Y), 20, 26, 43, Item.velocity.X, Item.velocity.Y, 100, Color.Yellow, Main.rand.NextFloat(.2f, .4f))];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.fadeIn = 1.3f;
            }

            Color color = Color.White * 0.5f;
            Texture2D texture = (Texture2D)Mod.Assets.Request<Texture2D>("Items/StaminaVessel_Glow");
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }
    }
}
