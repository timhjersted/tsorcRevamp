using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class FlareTome : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A lost legendary tome.");
        }

        public override void SetDefaults() {
            item.damage = 100;
            item.height = 10;
            item.knockBack = 4;
            item.rare = ItemRarityID.Pink;
            item.shootSpeed = 18;
            item.magic = true;
            item.noMelee = true;
            item.mana = 50;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 10;
            item.useAnimation = 10;
            item.value = 50000000;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.GreatFireballBall>();
            item.autoReuse = true;
        }
    }
}
