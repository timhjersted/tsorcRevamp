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
            item.useAnimation = 14;
            item.useTime = 14;
            item.autoReuse = true;
            item.maxStack = 1;
            item.damage = 199;
            item.knockBack = 5;
            item.UseSound = SoundID.Item1;
            item.shootSpeed = 21;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.value = PriceByRarity.Red_10;
            item.melee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.ForgottenRisingSun>();
            item.rare = ItemRarityID.Red;
        }

        public override bool CanUseItem(Player player) {
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.ForgottenRisingSun>()] < 10;
        }
    }
}
