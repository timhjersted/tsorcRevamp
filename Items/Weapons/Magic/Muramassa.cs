using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Muramassa : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A sword crafted for magic users" +
                               "\nDeals +1 damage for every 20 mana the user has over 200" +
                               "\nCan be upgraded with 25,000 Dark Souls & 3 Souls of Light");
        }

        public override void SetDefaults() {
            item.width = 48;
            item.height = 48;
            item.useAnimation = 18;
            item.useTime = 18;
            item.damage = 12;
            item.knockBack = 3;
            item.autoReuse = true;
            item.useTurn = true;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Green;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 27000;
            item.magic = true;
            item.mana = 5;
            item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            item.shootSpeed = 11f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Muramasa, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 5000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void ModifyWeaponDamage(Player player, ref float add, ref float mult, ref float flat)
        {
            if (player.statManaMax2 >= 200)
            {
                flat += (player.statManaMax2 - 200) / 20;
            }
        }
    }
}
