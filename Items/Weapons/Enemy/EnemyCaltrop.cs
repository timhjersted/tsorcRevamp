using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Enemy
{
    public class EnemyCaltrop : ModItem
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/Caltrop";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.width = 20;
            Item.height = 20;
            Item.damage = 18;
            Item.knockBack = 1.5f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<global::tsorcRevamp.Projectiles.Enemy.Weapons.EnemyCaltrop>();
            Item.shootSpeed = 8f;
            Item.DamageType = DamageClass.Ranged;
            Item.value = 0;
        }
    }
}
