using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Damage.ShadowmoonCloak
{
    [AutoloadEquip(EquipType.Back)]

    public class ShadowmoonCloakItem : ModItem
    {
        public static float DamageAndCritIncrease1 = 5f;
        public const float LifeThreshold = 40f;
        public static int ManaRegenBonus = 5;
        public static float DamageAndCritIncrease2 = 8f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageAndCritIncrease1, LifeThreshold, ManaRegenBonus, DamageAndCritIncrease2);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.Pink_5;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 25000);
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            recipe.AddIngredient(ItemID.StarCloak, 1);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<ShadowmoonCloakPlayer>();
            modPlayer.Equipped = true;
            modPlayer.Visual = !hideVisual;
        }

        public override void UpdateVanity(Player player)
        {
            if (player.direction == 1)
            {
                if (Main.rand.NextBool(8) && player.velocity.X > .5f)
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X - 6, player.position.Y + 16), 4, 24, 68, 0f, 0f, 30, default(Color), .5f)];
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.fadeIn = 1f;
                    dust.velocity += player.velocity;
                }
            }
            if (player.direction == -1)
            {
                if (Main.rand.NextBool(8) && player.velocity.X < -.5f)
                {
                    Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + 18, player.position.Y + 16), 4, 24, 68, 0f, 0f, 30, default(Color), .5f)];
                    dust.velocity *= 0f;
                    dust.noGravity = true;
                    dust.fadeIn = 1f;
                    dust.velocity += player.velocity;
                }
            }
        }
    }
}
