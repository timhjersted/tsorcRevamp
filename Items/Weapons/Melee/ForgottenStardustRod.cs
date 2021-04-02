using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenStardustRod : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Randomly casts Meteor.");
        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.Orange;
            item.damage = 70;
            item.height = 44;
            item.knockBack = 4;
            item.autoReuse = true;
            item.melee = true;
            item.useAnimation = 27;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 20000000;
            item.width = 46;
        }

        public override bool UseItem(Player player) {
            if (Main.rand.Next(4) == 0) {
                Projectile.NewProjectile(
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
