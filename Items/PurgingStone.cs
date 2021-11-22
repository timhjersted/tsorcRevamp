using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class PurgingStone : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Ash-colored stone encasing a skull." +
                                "\nSecret treasure of Arstor, the Earl of Carim." +
                                "\nAbsorbs curse build-up and breaks curse, restoring your max HP to 500." +
                                "\nCan not be used while potion sickness is active, and inflicts it." +
                                "\nHumans are helpless against curses, and can only redirect their influence." +
                                "\nThe Purging Stone does not dispel curses, but receives them as a surrogate." +
                                "\nThe stone itself was once a person or some other being.");
        }

        public override void SetDefaults() {
            item.width = 38;
            item.height = 38;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useAnimation = 15;
            item.useTime = 15;
            item.maxStack = 10;
            item.healLife = 500;
            item.consumable = true;
            item.scale = 1;
            item.UseSound = SoundID.Item4;
            item.rare = ItemRarityID.Pink;
            item.value = PriceByRarity.Pink_5;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("RedTitanite"), 5);
            recipe.AddIngredient(mod.GetItem("WhiteTitanite"), 5);
            recipe.AddIngredient(mod.GetItem("BlueTitanite"), 5);
            recipe.AddIngredient(mod.GetItem("CursedSoul"), 30);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool CanUseItem(Player player) {

            if((player.statLifeMax < 500) || player.HasBuff(ModContent.BuffType<Buffs.CurseBuildup>()) || player.HasBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>()))
            {
                if (!player.HasBuff(BuffID.PotionSickness))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool UseItem(Player player) {
            if (player.statLifeMax < 500) {
                player.statLifeMax = 500;
            }

            player.statLife += 500;
            player.AddBuff(BuffID.PotionSickness, 10800);
            player.HealEffect(500);

            int buffIndex = 0;

            foreach (int buffType in player.buffType) {

                if (buffType == ModContent.BuffType<Buffs.CurseBuildup>() || buffType == ModContent.BuffType<Buffs.PowerfulCurseBuildup>()) {
                    player.DelBuff(buffIndex);
                    player.GetModPlayer<tsorcRevampPlayer>().CurseLevel = 0;
                    player.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel = 0;
                }
                buffIndex++;
            }
            return true;
        }
    }
}
