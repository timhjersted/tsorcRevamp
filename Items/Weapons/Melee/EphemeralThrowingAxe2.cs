using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class EphemeralThrowingAxe2 : ModItem {

        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/EphemeralThrowingAxe";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("An enchanted melee weapon that can be thrown through walls.\n" + "It does double damage against mages and other magic users.");
        }
        public override void SetDefaults() {
            item.consumable = false;
            item.damage = 32;
            item.height = 34;
            item.knockBack = 7;
            item.maxStack = 1;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.melee = true;
            item.shootSpeed = 10;
            item.useAnimation = 19;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 19;
            item.value = 150000;
            item.width = 22;
            item.shoot = ModContent.ProjectileType<Projectiles.EphemeralThrowingAxe2>();
        }
    }
}
