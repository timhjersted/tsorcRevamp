using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Enemy
{
    public class EnemySmokeBomb : ModItem
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemySmokebomb";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.width = 22;
            Item.height = 22;
            Item.damage = 1;
            Item.knockBack = 0f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.Enemy.Weapons.InvaderSmokeBomb>();
            Item.shootSpeed = 7.2f;
            Item.DamageType = DamageClass.Ranged;
            Item.value = 0;
        }
    }
}
