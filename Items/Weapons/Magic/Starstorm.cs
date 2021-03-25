using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Starstorm : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Causes stars to storm from the sky");
        }
        public override void SetDefaults() {
            item.width = 42;
            item.height = 42;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 15;
            item.useTime = 15;
            item.damage = 45;
            item.knockBack = 6;
            item.autoReuse = true;
            item.alpha = 100;
            item.scale = 1.15f;
            item.UseSound = SoundID.Item9;
            item.rare = ItemRarityID.Orange;
            item.mana = 13;
            item.value = 70000;
            item.magic = true;
        }


        public override bool UseItem(Player player) {
            float x = (float)(Main.mouseX + Main.screenPosition.X);
            float y = (float)(Main.mouseY + Main.screenPosition.Y);
            float speedX = (Main.rand.Next(80) - 40) / 10f;
            float speedY = 14.9f;
            int type = ProjectileID.Starfury;
            int damage = item.damage;
            float knockback = 3.0f;
            int owner = player.whoAmI;
            y += -500f;

            for (int i = 0; i < 5; i++) {
                Projectile.NewProjectile(x + ((i * 40) - 80), y, speedX, speedY, type, damage, knockback, owner);
            }
            return true;
        }
    }
}
