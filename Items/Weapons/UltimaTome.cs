using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class UltimaTome : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Ultimate tome guarded by the Omega Weapon.");
        }
        public override void SetDefaults() {
            item.damage = 500;
            item.height = 10; 
            item.knockBack = 4;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 6;
            item.noMelee = true;
            item.magic = true;
            item.mana = 200;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 10;
            item.useAnimation = 10;
            item.value = 5000000;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Ultima>();
        }
    }
}
