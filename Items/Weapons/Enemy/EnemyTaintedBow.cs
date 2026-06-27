using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Enemy
{
    public class EnemyTaintedBow : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/Bows/TaintedBow";

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyTaintedArrow>();
            Item.damage = 24;
            Item.height = 46;
            Item.width = 18;
            Item.knockBack = 1.25f;
            Item.noMelee = true;
            Item.rare = ItemRarityID.White;
            Item.shootSpeed = 11.3f;
            Item.useAmmo = AmmoID.Arrow;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = 0;
        }
    }
}
