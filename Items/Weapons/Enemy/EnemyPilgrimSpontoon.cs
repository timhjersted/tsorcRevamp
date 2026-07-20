using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Enemy
{
    public class EnemyPilgrimSpontoon : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Spears/PilgrimSpontoon";

        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 52;
            Item.damage = 1;
            Item.knockBack = 3f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 27;
            Item.useTime = 27;
            Item.UseSound = SoundID.Item71;
            Item.shoot = ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyPilgrimArcaneBall>();
            Item.shootSpeed = 10f;
            Item.DamageType = DamageClass.Melee;
            Item.rare = ItemRarityID.White;
            Item.value = 0;
        }
    }
}
