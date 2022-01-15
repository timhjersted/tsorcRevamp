using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Murassame : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A sword crafted for magic users" +
                               "\nDeals +1 damage for every 10 mana the user has over 200");
        }

        public override void SetDefaults() {
            item.width = 48;
            item.height = 48;
            item.useAnimation = 16;
            item.useTime = 16;
            item.damage = 18;
            item.knockBack = 5;
            item.autoReuse = true;
            item.useTurn = true;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.LightRed;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = PriceByRarity.LightRed_4;
            item.magic = true;
            item.mana = 5;
            item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            item.shootSpeed = 12f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(mod.GetItem("Muramassa"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 5000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void ModifyWeaponDamage(Player player, ref float add, ref float mult, ref float flat)
        {
            if (player.statManaMax2 >= 200)
            {
                flat += (player.statManaMax2 - 200) / 10;
            }
        }
    }
}
