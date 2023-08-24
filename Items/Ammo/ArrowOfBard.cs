using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Items.Ammo
{
    public class ArrowOfBard : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.ammo = AmmoID.Arrow;
            Item.shoot = ModContent.ProjectileType<ArrowOfBardProjectile>();
            Item.damage = 50; //500 totally was just a typo guys, how did we not notice this earlier
            Item.height = 28;
            Item.knockBack = 4f;
            Item.maxStack = 9999;
            Item.DamageType = DamageClass.Ranged;
            Item.shootSpeed = 3.5f;
            Item.useAnimation = 100;
            Item.useTime = 100;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.width = 10;
            Item.shoot = ModContent.ProjectileType<ArrowOfBardProjectile>();
        }
    }
}
