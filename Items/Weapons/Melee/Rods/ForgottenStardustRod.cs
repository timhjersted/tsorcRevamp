using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Rods
{
    class ForgottenStardustRod : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Randomly casts Meteor.");
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Pink;
            Item.damage = 70;
            Item.height = 44;
            Item.knockBack = 4;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 27;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = PriceByRarity.Pink_5;
            Item.width = 46;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.rand.NextBool(4))
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item),
               (float)(Main.mouseX + Main.screenPosition.X) - 100 + Main.rand.Next(200),
               (float)(Main.mouseY + Main.screenPosition.Y) - 500.0f,
               (float)(-40 + Main.rand.Next(80)) / 10,
               14.9f,
               ModContent.ProjectileType<Projectiles.Meteor>(),
               50,
               2.0f,
               player.whoAmI);
            }
            return true;
        }
    }
}
