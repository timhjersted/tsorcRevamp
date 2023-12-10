using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class RingOfFavorAndProtection : ModItem
    {
        public static int MaxLifeIncrease = 20;
        public static int MaxStaminaIncrease = 10;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxLifeIncrease, MaxStaminaIncrease);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void UpdateEquip(Player player)
        {
            player.statLifeMax2 += MaxLifeIncrease;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 += 10f;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(Item.Right, 0.4f, 0.4f, 0.0f);

            if (Main.rand.NextBool(50))
            {
                Dust dust = Main.dust[Dust.NewDust(Item.position, Item.width, Item.height, 57, 0, 0, 100, default(Color), 1f)];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.velocity += Item.velocity;
                dust.fadeIn = 1.4f;
            }
        }
    }
}
