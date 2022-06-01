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
            Item.width = 48;
            Item.height = 48;
            Item.useAnimation = 16;
            Item.useTime = 16;
            Item.damage = 18;
            Item.knockBack = 5;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightRed;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 5;
            Item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            Item.shootSpeed = 12f;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(Mod.GetItem("Muramassa"), 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 5000);
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
