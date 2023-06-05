using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Tools
{
    public class HeavenPiercer : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 12;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.useAnimation = 25;
            Item.useTime = 7;
            Item.pick = 999;
            Item.axe = 199;
            Item.damage = 42;
            Item.knockBack = 5;
            Item.UseSound = SoundID.Item23;
            Item.shootSpeed = 36;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.HeavenPiercer>();
        }
    }
}