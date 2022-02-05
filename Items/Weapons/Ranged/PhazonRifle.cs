using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class PhazonRifle : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Three round burst \nOnly the first shot consumes ammo\nPhazon rounds are extremely volatile");
        }
        public override void SetDefaults() {
            item.width = 50;
            item.height = 18;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 12;
            item.useTime = 4;
            item.maxStack = 1;
            item.damage = 25;
            item.knockBack = 0f;
            item.autoReuse = true;
            item.UseSound = SoundID.Item31;
            item.rare = ItemRarityID.LightPurple;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 7;
            item.useAmmo = 14;
            item.noMelee = true;
            item.value = PriceByRarity.LightPurple_6;
            item.ranged = true;
            item.reuseDelay = 11;
            item.useAmmo = AmmoID.Bullet;
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            type = ModContent.ProjectileType<Projectiles.PhazonRound>();
            return true;
        }


        public override bool ConsumeAmmo(Player player)
        {
            return !(player.itemAnimation < item.useAnimation - 2); //consume 1 ammo instead of 3
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-8, 0);
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.ClockworkAssaultRifle);
            recipe.AddIngredient(ItemID.MeteoriteBar, 30);
            recipe.AddIngredient(ItemID.MythrilBar, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
