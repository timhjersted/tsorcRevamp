using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class TomeOfHealth : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Tome of Health");
            Tooltip.SetDefault("Spell tome that heals 220 HP" +
                                "\nShares cooldown with potions" +
                                "\nCannot be used with \"Quick Heal\"");
        }

        public override void SetDefaults() {
            item.height = 38;
            item.useTurn = true;
            item.rare = ItemRarityID.Pink;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.UseSound = SoundID.Item4;
            item.useAnimation = 17;
            item.useTime = 17;
            item.value = 27000;
            item.width = 28;
            item.mana = 240;
        }

        public override bool CanUseItem(Player player) {
            return (!player.HasBuff(BuffID.PotionSickness) && !player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse);
        }
        public override bool UseItem(Player player) {
            player.statLife += 220;
            if (player.statLife > player.statLifeMax2) player.statLife = player.statLifeMax2;
            player.HealEffect(220, true);
            player.AddBuff(BuffID.PotionSickness, 3600);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer)
            {
                tooltips.Add(new TooltipLine(mod, "BOTCNoHeal", "Doesn't heal the [c/6d8827:Bearer of the Curse]"));
            }
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome);
            recipe.AddIngredient(ItemID.LifeCrystal, 5);
            recipe.AddIngredient(ItemID.CrystalShard, 30);
            recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(ModContent.ItemType<Items.DarkSoul>(), 7000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
