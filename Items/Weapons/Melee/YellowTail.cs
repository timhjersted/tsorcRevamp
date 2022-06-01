using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class YellowTail : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Shatters strong defenses with high penetration(multi - hit damage)");
        }

        public override void SetDefaults() {
            Item.width = 35;
            Item.height = 35;
            Item.useStyle = ItemUseStyleID.Thrust;
            Item.useAnimation = 15;
            Item.autoReuse = true;
            Item.useTime = 15;
            Item.maxStack = 1;
            Item.damage = 17;
            Item.knockBack = 4;
            Item.scale = 1;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.Blue_1;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GoldShortsword, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 2000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}
