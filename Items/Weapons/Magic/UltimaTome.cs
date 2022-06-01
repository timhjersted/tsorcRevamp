using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class UltimaTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Ultimate tome guarded by the Omega Weapon.");
        }
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.height = 10;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 6;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 200;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.value = 5000000;
            Item.width = 34;
            Item.shoot = ModContent.ProjectileType<Projectiles.Ultima>();
        }
    }
}
