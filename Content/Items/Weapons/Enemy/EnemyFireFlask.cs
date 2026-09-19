using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Ranged;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class EnemyFireFlask : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(FireFlaskProj));

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.width = 22;
            Item.height = 24;
            Item.damage = 20;
            Item.knockBack = 3f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<global::tsorcRevamp.Content.Projectiles.Enemy.Weapons.EnemyFireFlask>();
            Item.shootSpeed = 7.5f;
            Item.DamageType = DamageClass.Ranged;
            Item.value = 0;
        }
    }
}
