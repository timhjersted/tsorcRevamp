using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Accessories;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive.Rings
{
    public class BarrierRing : ModItem
    {
        public static int Cooldown = 75;
        public static float ImmuneTimeAfterHit = 1f; //in seconds
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Cooldown);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            //recipe.AddIngredient(ItemID.SoulofLight, 20);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().BarrierRing = true;
            if (!player.HasBuff(ModContent.BuffType<BarrierCooldown>()))
            {
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, player.velocity, ModContent.ProjectileType<Projectiles.Barrier>(), 0, 0f, player.whoAmI);
                Lighting.AddLight(player.Center, .450f, .450f, .600f);
            }
        }

    }
}
