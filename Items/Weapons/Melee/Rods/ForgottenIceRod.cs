using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Rods
{
    class ForgottenIceRod : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Randomly casts ice 2.");
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Blue;
            Item.damage = 20;
            Item.height = 26;
            Item.knockBack = 4;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.useAnimation = 27;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = PriceByRarity.Blue_1;
            Item.width = 26;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.rand.NextBool(5))
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.position.X, player.position.Y, (float)(-40 + Main.rand.Next(80)) / 10, 14.9f, ModContent.ProjectileType<Projectiles.Ice2Ball>(), 20, 2.0f, player.whoAmI);
            }
            return true;
        }
    }
}
