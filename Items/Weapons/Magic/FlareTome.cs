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
            Item.damage = 100;
            Item.height = 10;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = 18;
            Item.magic = true;
            Item.noMelee = true;
            Item.mana = 50;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.value = PriceByRarity.Red_10;
            Item.width = 34;
            Item.shoot = ModContent.ProjectileType<Projectiles.GreatFireballBall>();
            Item.autoReuse = true;
        }
    }
}
