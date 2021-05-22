using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Pets {
    class Miakoda : ModItem {
        public override bool Autoload(ref string name) => ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Miakoda - an ancient being of light over 100 years old, " +
                                "\nwho has vowed to help you find your wife and defeat Attraidies." +
                                "\nMiakoda is an indigenous name that means \"Power of the Moon\".");
        }
        public override void SetDefaults() {
            item.CloneDefaults(ItemID.DD2PetGhost);
            item.shoot = ModContent.ProjectileType<Projectiles.Pets.Miakoda>();
            item.buffType = ModContent.BuffType<Buffs.Miakoda>();
        }

        public override void UseStyle(Player player) {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0) {
                player.AddBuff(item.buffType, 3600, true);
            }
        }
    }
}
