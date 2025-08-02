using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic.Scrolls
{
    class GreatEnergyBeamScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("The scroll reads \"Exevo gran vix lux.\"");
        }
        public override void SetDefaults()
        {
            Item.damage = 150;
            Item.height = 10;
            Item.knockBack = 1;
            Item.rare = ItemRarityID.Red;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 32;
            Item.useAnimation = 26;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 26;
            Item.value = PriceByRarity.Red_10;
            Item.width = 34;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.GreatEnergyBeam>();
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            position = Main.MouseWorld;
        }
    }
}
