using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Throwing;

namespace tsorcRevamp.Items.Weapons.Throwing
{
    class OilPot : ModItem
    {
        public static int OiledDurationInSeconds = 10;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.width = 22;
            Item.damage = 10;
            Item.height = 24;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 8f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = 500;
            Item.shoot = ModContent.ProjectileType<OilPotProjectile>();
            Item.shootSpeed = 6.5f;
            Item.useAnimation = 50;
            Item.useTime = 50;
            Item.UseSound = SoundID.Item1;
            Item.consumable = true;
            Item.maxStack = 999;
            Item.DamageType = DamageClass.Throwing;
        }
    }
}