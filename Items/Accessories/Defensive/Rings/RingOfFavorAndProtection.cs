using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Accessories.Defensive.Rings
{
    public class RingOfFavorAndProtection : ModItem
    {
        public const int MaxLifeIncrease = 40;
        public const int MaxStaminaIncrease = 15;
        /// <summary>Percentage points of shield-hold movement slow this ring cancels (see
        /// tsorcRevampActiveShieldPlayer.ApplyBlockSlow). </summary>
        public const float ShieldSlowReduction = 5f;
        /// <summary>Percentage points added to the normal stamina regen retained while a shield is raised.</summary>
        public const float BlockStaminaRegenBonus = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxLifeIncrease, MaxStaminaIncrease,  ShieldSlowReduction, BlockStaminaRegenBonus);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateEquip(Player player)
        {
            var staminaPlayer  = player.GetModPlayer<tsorcRevampStaminaPlayer>();
            player.statLifeMax2 += MaxLifeIncrease;
            staminaPlayer.staminaResourceMax2 += MaxStaminaIncrease;
            staminaPlayer.RingOfFavor = true;
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
