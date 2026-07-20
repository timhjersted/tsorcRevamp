using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Enemy
{
    public class EnemyNinjaStar : ModItem
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/NinjaStarProj";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.width = 28;
            Item.height = 28;
            Item.damage = 24;
            Item.knockBack = 2f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyNinjaStarProj>();
            Item.shootSpeed = 10f;
            Item.DamageType = DamageClass.Ranged;
            Item.value = 0;
        }
    }
}
