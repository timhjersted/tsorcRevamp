using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items
{
    class StaminaVessel : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Permanently increases max stamina by 5%" +
                               "\nStamina maxes out after consuming 10");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 24;
            item.rare = ItemRarityID.Expert;
            item.value = 0;
            item.useAnimation = 60;
            item.useTime = 60;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.UseSound = SoundID.Item4;
            item.maxStack = 99;
            item.consumable = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;

            tooltips.Insert(4, new TooltipLine(mod, "", $"Consumed so far: { (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax - 100) / 5}"));

        }

        public override bool CanUseItem(Player player)
        {
            return (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax < 150);
        }

        public override bool UseItem(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax += 5;
            return true;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(item.Center, 0.15f, 0.25f, 0.15f);

            if (Main.rand.Next(20) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(item.position.X, item.position.Y), 20, 26, 43, item.velocity.X, item.velocity.Y, 100, Color.Yellow, Main.rand.NextFloat(.2f, .4f))];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.fadeIn = 1.3f;
            }

            Color color = Color.White * 0.5f;
            Texture2D texture = mod.GetTexture("Items/StaminaVessel_Glow");
            spriteBatch.Draw(texture, new Vector2(item.position.X - Main.screenPosition.X + item.width * 0.5f, item.position.Y - Main.screenPosition.Y + item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}
