using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenIceBrand : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A sword imbued with ice.\n" + "Will randomly cast ice 2.");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Pink;
            item.damage = 52;
            item.height = 42;
            item.knockBack = 4;
            item.autoReuse = true;
            item.melee = true;
            item.scale = 1.05f;
            item.useAnimation = 22;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 15;
            item.value = 6000000;
            item.width = 42;
        }

        public override bool UseItem(Player player) {
            if (Main.rand.Next(5) == 0) {
                Projectile.NewProjectile(player.position.X, player.position.Y, (float)(-40 + Main.rand.Next(80)) / 10, 14.9f, ModContent.ProjectileType<Projectiles.Ice2Ball>(), 20, 2.0f, player.whoAmI);
            }
            return true;
        }
    }
}
