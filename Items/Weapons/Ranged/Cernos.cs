using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    public class Cernos : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Fires three arrows. \nHold FIRE to charge. \nArrows are faster and more accurate when the bow is charged.");
        }

        public override void SetDefaults() {
            item.ranged = true;
            item.shoot = ModContent.ProjectileType<Projectiles.CernosHeld>();
            item.channel = true;

            item.damage = 230;
            item.width = 24;
            item.height = 48;
            item.useTime = 8;
            item.useAnimation = 8;
            item.reuseDelay = 4;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.knockBack = 4f;
            item.value = Item.sellPrice(0, 35);
            item.rare = ItemRarityID.Lime;
            item.UseSound = SoundID.Item43;

            item.shootSpeed = 18f;

        }

        public override bool CanUseItem(Player player) {
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.CernosHeld>()] <= 0;
        }
    }
}
