using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic.Scrolls
{
    class FlameStrikeScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("The scroll reads \"Exori flam.\"");
        }
        public override void SetDefaults()
        {
            Item.damage = 25;
            Item.height = 10;
            Item.knockBack = 1;
            Item.rare = ItemRarityID.LightRed;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 12;
            Item.UseSound = SoundID.Item20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.value = PriceByRarity.LightRed_4;
            Item.width = 34;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.Scrolls.FlameScrollStrike>();
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // projectile at the cursor position
            position = Main.MouseWorld;
        }
    }
}
