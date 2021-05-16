using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Head)]
    public class DragoonHelmet2 : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Dragoon Helmet II");
            Tooltip.SetDefault("Harmonized with Sky and Fire\n+200 Mana\nPotion use has a 15 second shorter cooldown.");
        }

        public override void SetDefaults() {
            item.width = 18;
            item.height = 12;
            item.defense = 15;
            item.value = 10000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player) {
            player.statManaMax2 += 200;
            player.pStone = true;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs) {
            return body.type == ModContent.ItemType<DragoonArmor2>() && legs.type == ModContent.ItemType<DragoonGreaves2>();
        }

        public override void UpdateArmorSet(Player player) {
            player.setBonus = "Harmonized with the four elements: fire, water, earth and air, including +6 life regen and 38% boost to all stats";
            player.lavaImmune = true;
            player.fireWalk = true;
            player.breath = 9999999;
            player.waterWalk = true;
            player.noKnockback = true;
            player.meleeDamage += 0.38f;
            player.magicDamage += 0.38f;
            player.rangedDamage += 0.38f;
            player.rangedCrit += 38;
            player.meleeCrit += 38;
            player.magicCrit += 38;
            player.meleeSpeed += 0.38f;
            player.moveSpeed += 0.38f;
            player.manaCost -= 0.38f;
            player.lifeRegen += 6;
            player.eocDash = 20;
            player.armorEffectDrawShadowEOCShield = true;

            player.wings = 34; // looks like Jim's Wings
            player.wingsLogic = 34;
            player.wingTimeMax = 180;

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DragoonHelmet"), 1);
            recipe.AddIngredient(mod.GetItem("DragonScale"), 10);
            recipe.AddIngredient(mod.GetItem("DyingWindShard"), 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 40000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
