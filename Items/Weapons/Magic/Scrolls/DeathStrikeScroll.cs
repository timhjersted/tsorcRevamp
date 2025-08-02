using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic.Scrolls
{
    class DeathStrikeScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("The scroll reads \"Exori mort.\"");
        }
        public override void SetDefaults()
        {
            Item.damage = 50;
            Item.height = 10;
            Item.knockBack = 1;
            Item.rare = ItemRarityID.Pink;
            Item.shootSpeed = 0;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 28;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item103;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 30;
            Item.value = PriceByRarity.Pink_5;
            Item.width = 34;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.SuddenDeathStrike>();
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // projectile at the cursor position
            position = Main.MouseWorld;
        }
    }
}
