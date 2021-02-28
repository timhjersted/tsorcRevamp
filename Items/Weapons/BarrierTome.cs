using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    public class BarrierTome : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Barrier Tome");
            Tooltip.SetDefault("A lost tome for artisans.\n" +
                                "Casts Barrier on the wearer, which adds 20 defense for 20 seconds.\n" +
                                "Does not stack with other Barrier spells.");

        }

        public override void SetDefaults() {
            item.stack = 1;
            item.width = 34;
            item.height = 10;
            item.maxStack = 1;
            item.rare = ItemRarityID.Orange;
            item.magic = true;
            item.noMelee = true;
            item.mana = 150;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 10;
            item.useAnimation = 10;
            item.value = 42000;

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.SoulofSight, 10);
            recipe.AddIngredient(ItemID.SoulofLight, 20);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player) {
            player.AddBuff(ModContent.BuffType<Buffs.Protect>(), 60, false);
            return true;
        }

    }
}
