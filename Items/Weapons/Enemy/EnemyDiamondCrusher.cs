using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Enemy
{
    public class EnemyDiamondCrusher : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Flails/DiamondCrusher";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.damage = 23;
            Item.knockBack = 5f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.White;
            Item.shootSpeed = 10f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = 0;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }
    }
}
