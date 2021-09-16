using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Ammo {
    public class PowerBolt : ModItem {

        public override void SetDefaults() {
            item.consumable = true;
            item.ranged = true;
            item.ammo = mod.ItemType("Bolt");
            item.damage = 20;
            item.height = 28;
            item.knockBack = 3f;
            item.maxStack = 2000;
            item.shootSpeed = 3.5f;
            item.value = 1000;
            item.shoot = ModContent.ProjectileType<Projectiles.Bolt>();
        }
    }
}
