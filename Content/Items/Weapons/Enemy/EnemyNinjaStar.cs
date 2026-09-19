using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class EnemyNinjaStar : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyNinjaStarProj));

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
