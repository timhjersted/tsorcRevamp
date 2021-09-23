using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class WallTome : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Wall Tome");
            Tooltip.SetDefault("A lost tome that is consumed on use\n" +
                               "Casts Wall on the player, raising defense by 25 for 25 seconds" +
                               "\nDoes not stack with Fog, Barrier or Shield spells");
        }

        bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetDefaults() {
            item.stack = 1;
            item.width = 28;
            item.height = 30;
            item.maxStack = 99;
            item.rare = ItemRarityID.Green;
            item.magic = true;
            item.noMelee = true;
            item.mana = 50;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 35;
            item.useAnimation = 35;
            item.value = 8000;
            item.consumable = true;

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.IronskinPotion);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 600);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player) {
            player.AddBuff(ModContent.BuffType<Buffs.Wall>(), 1500, false);
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                player.AddBuff(ModContent.BuffType<Buffs.ShieldCooldown>(), 5100); //85 seconds (60 seconds downtime)
            }
            return true;
        }
        public override bool CanUseItem(Player player) {
            if (!LegacyMode) { //in revamp mode
                if (player.HasBuff(ModContent.BuffType<Buffs.ShieldCooldown>())) {
                    return false;
                }
            }
            if (player.HasBuff(ModContent.BuffType<Buffs.Fog>()) || player.HasBuff(ModContent.BuffType<Buffs.Barrier>()) || player.HasBuff(ModContent.BuffType<Buffs.Shield>())) {
                return false;
            }
            else {
                return true;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            if (!LegacyMode) {
                tooltips.Add(new TooltipLine(mod, "RevampWallNerf1", "Reduces damage dealt by 20% and movement speed by 15%"));
                tooltips.Add(new TooltipLine(mod, "RevampCDNerf1", "Cannot be used again for 60 seconds after wearing off"));

            }
        }
    }
}