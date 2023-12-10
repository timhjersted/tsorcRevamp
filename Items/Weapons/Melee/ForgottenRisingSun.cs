using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class ForgottenRisingSun : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 14;
            Item.useTime = 14;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.damage = 300;
            Item.crit = 26;
            Item.knockBack = 5;
            Item.UseSound = SoundID.Item1;
            Item.shootSpeed = 21;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.ForgottenRisingSunProj>();
            Item.rare = ItemRarityID.Red;

            Item.mana = 15;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.ForgottenRisingSunProj>()] < 10;
        }
    }
}
