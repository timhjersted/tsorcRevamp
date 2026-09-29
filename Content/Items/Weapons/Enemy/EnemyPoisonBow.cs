using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class EnemyPoisonBow : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Weapons/Enemy/PoisonBow";

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<ShadowNinjaPoisonArrow>();
            Item.damage = 24;
            Item.width = 28;
            Item.height = 58;
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
