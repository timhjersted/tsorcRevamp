using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    // Display prop for Ulhan's throw; the projectile uses the same 24x24 art.
    public class EnemyProximityMine : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/Weapons/ProximityMine";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.width = 24;
            Item.height = 24;
            Item.damage = 50;
            Item.knockBack = 1.5f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.Enemy.Weapons.ProximityMine>();
            Item.shootSpeed = 8f;
            Item.DamageType = DamageClass.Ranged;
            Item.value = 0;
        }
    }
}
