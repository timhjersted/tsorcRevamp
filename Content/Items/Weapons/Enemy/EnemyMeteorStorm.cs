using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class EnemyMeteorStorm : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(MeteorStorm));

        public override void SetStaticDefaults()
        {
            Item.staff[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 64;
            Item.damage = 1;
            Item.knockBack = 4f;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 11;
            Item.useTime = 11;
            Item.UseSound = SoundID.Item88;
            Item.shoot = ModContent.ProjectileType<Projectiles.Enemy.EnemyMeteorStormMeteor>();
            Item.shootSpeed = 3f;
            Item.DamageType = DamageClass.Magic;
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }
    }
}
