using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items {
    class RTQ2 : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("RTQ2");
            Tooltip.SetDefault("Vanity pet" +
                                "\nIt's cute, but not fluffy.");
        }
        public override void SetDefaults() {
            item.CloneDefaults(ItemID.DD2PetGhost);
            item.shoot = ModContent.ProjectileType<Projectiles.Pets.RTQ2>();
            item.buffType = ModContent.BuffType<Buffs.RTQ2>();
        }

        public override void UseStyle(Player player) {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0) {
                player.AddBuff(item.buffType, 3600, true);
            }
        }
    }
}
