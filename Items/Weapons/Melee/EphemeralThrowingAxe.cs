using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class EphemeralThrowingAxe : ModItem {
        public override void SetDefaults() {
            item.damage = 22;
            item.height = 34;
            item.knockBack = 7;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.melee = true;
            item.shootSpeed = 8;
            item.useAnimation = 19;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 19;
            item.value = 150000;
            item.width = 22;
            item.shoot = ModContent.ProjectileType<Projectiles.EphemeralThrowingAxe>();
        }
    }
}
