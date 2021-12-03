using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    class RingOfClarity : ModItem {

        public override string Texture => "tsorcRevamp/Items/Accessories/PoisonbloodRing";
        public override void SetStaticDefaults() {
            string defString = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode ? "" : " and 12 defense";
            Tooltip.SetDefault("The Ring of Clarity prevents confusion, gravitation disorientation, bleeding, and poisoning. \n+4 HP Regeneration" + defString);
        }
        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.useAnimation = 100;
            item.useTime = 100;
            item.maxStack = 1;
            item.rare = ItemRarityID.Orange;
            item.value = PriceByRarity.Orange_3;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<PoisonbloodRing>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {

            player.buffImmune[BuffID.Confused] = true;
            player.buffImmune[BuffID.Gravitation] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.buffImmune[BuffID.Poisoned] = true;

            player.lifeRegen += 4;
            player.statDefense += ModContent.GetInstance<tsorcRevampConfig>().LegacyMode ? 0 : 12;
        }
    }
}
