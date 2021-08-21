using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    public class CernosPrime : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Fires three arrows. \nHold FIRE to charge. \nArrows are faster and more accurate when the bow is charged.");
        }

        public override void SetDefaults() {
            item.ranged = true;
            item.shoot = ModContent.ProjectileType<Projectiles.CernosPrimeHeld>();
            item.channel = true;

            item.damage = 666; //not a meme, actually balanced 
            item.width = 24;
            item.height = 48;
            item.useTime = 48; //unused, but set to match the exact max charge time so the use time displays as "Extremely Slow"
            item.useAnimation = 48; //''
            item.reuseDelay = 4;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.knockBack = 4f;
            item.value = Item.sellPrice(0, 35);
            item.rare = ItemRarityID.Lime;
            item.UseSound = SoundID.Item7;

            item.shootSpeed = 18f;

            //item.useAmmo = AmmoID.Arrow; //dont do this! it'll just shoot the arrow instead of using the bow draw animation.
            //TODO investigate displaying the ammo count on the bow

        }

        public override bool CanUseItem(Player player) {
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.CernosPrimeHeld>()] <= 0;
        }

    }
}
