using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenRisingSun : ModItem {

        public override void SetDefaults() {
            item.width = 22;
            item.height = 22;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 15;
            item.useTime = 15;
            item.autoReuse = true;
            item.maxStack = 10;
            item.damage = 248;
            item.knockBack = 5;
            item.UseSound = SoundID.Item1;
            item.shootSpeed = 21;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.value = 17000;
            item.melee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.ForgottenRisingSun>();
        }

        public override bool CanUseItem(Player player) {
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.ForgottenRisingSun>()] < 10;
        }
    }
}
