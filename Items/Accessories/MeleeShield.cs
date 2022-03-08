using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public abstract class MeleeShield : ModItem {
        //all the melee shields have a lot in common, so i use an abstract class from which they inherit values
        //i dont feel like writing the same thing 4 times. does it make the code less readable? yeah. i dont give a shit
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("For melee warriors only" + 
                                "\nGrants immunity to knockback");
        }
        public override void SetDefaults() {
            item.width = 30;
            item.height = 30;
            item.accessory = true;
            item.rare = ItemRarityID.Orange;
        }
        public override void UpdateEquip(Player player) {
            player.noKnockback = true;
            player.fireWalk = true;
            player.manaCost += 0.7f;
        }
        public override bool CanEquipAccessory(Player player, int slot) {
            foreach (Item i in player.armor) {
                if (i.type == ModContent.ItemType<GazingShield>()
                    || i.type == ModContent.ItemType<BeholderShield>()
                    || i.type == ModContent.ItemType<BeholderShield2>()
                    || i.type == ModContent.ItemType<EnchantedBeholderShield2>()) {
                    return false;
                }
            }

            return base.CanEquipAccessory(player, slot);
        }
    }

    public class GazingShield : MeleeShield {

        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
            int ttindex = tooltips.FindLastIndex(t => t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
            if (ttindex != -1) {// if we find one
                                //insert the extra tooltip line
                tooltips.Insert(ttindex + 1, new TooltipLine(mod, "",
                "Plus 20 defense and 4% melee damage" +
                "\nReduces Ranged, Magic and Summoner Damage by 85%. +70% mana cost" +
                "\n+5% move speed"));
            }
        }

        public override void SetDefaults() {
            base.SetDefaults();
            item.defense = 20;
            item.value = PriceByRarity.LightRed_4;
            item.rare = ItemRarityID.LightRed;
        }


        public override void UpdateEquip(Player player) {
            base.UpdateEquip(player);
            player.moveSpeed += 0.05f;
            player.manaCost += 0.7f; 
            player.meleeDamage += 0.04f;
            player.magicDamage -= 0.85f;
            player.rangedDamage -= 0.85f;
            player.minionDamage -= 0.85f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MythrilBar, 15);
            recipe.AddIngredient(ItemID.SoulofLight, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();

        }

    }

    public class BeholderShield : MeleeShield {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
            int ttindex = tooltips.FindLastIndex(t => t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
            if (ttindex != -1) {// if we find one
                                //insert the extra tooltip line
                tooltips.Insert(ttindex + 1, new TooltipLine(mod, "",
                "Plus 40 defense and 6% melee damage" +
                "\nReduces Ranged, Magic and Summoner Damage by 150%. +70% mana cost" +
                "\n+10% move speed"));
            }
        }
        public override void SetDefaults() {
            base.SetDefaults();
            item.defense = 40;
            item.value = PriceByRarity.Pink_5;
            item.rare = ItemRarityID.Pink;
        }

        public override void UpdateEquip(Player player) {
            base.UpdateEquip(player);
            player.moveSpeed += 0.10f;
            player.meleeDamage += 0.06f;
            player.magicDamage -= 1.5f;
            player.rangedDamage -= 1.5f;
            player.minionDamage -= 1.5f;
            player.manaCost += 0.7f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("GazingShield"), 1);
            recipe.AddIngredient(ItemID.SoulofMight, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 30000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();

        }
    }

    public class BeholderShield2 : MeleeShield {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Beholder Shield II");
            base.SetStaticDefaults();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
            int ttindex = tooltips.FindLastIndex(t => t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
            if (ttindex != -1) {// if we find one
                                //insert the extra tooltip line
                tooltips.Insert(ttindex + 1, new TooltipLine(mod, "",
                "Plus immunity to On Fire, 60 defense, and 6% melee damage" +
                "\nReduces Ranged, Magic and Summoner Damage by 150%. +70% mana cost" +
                "\n+15% move speed"));
            }
        }
        public override void SetDefaults() {
            base.SetDefaults();
            item.defense = 60;
            item.value = PriceByRarity.Cyan_9;
            item.rare = ItemRarityID.Cyan;
        }

        public override void UpdateEquip(Player player) {
            base.UpdateEquip(player);
            player.moveSpeed += 0.15f;
            player.meleeDamage += 0.06f;
            player.magicDamage -= 1.5f;
            player.rangedDamage -= 1.5f;
            player.minionDamage -= 1.5f;
            player.manaCost += 0.7f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("BeholderShield"), 1);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"), 1);
            recipe.AddIngredient(mod.GetItem("SoulOfAttraidies"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 60000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();

        }
    }

    public class EnchantedBeholderShield2 : MeleeShield {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Enchanted Beholder Shield II");
            Tooltip.SetDefault("A legendary shield for melee warriors only" +
                "\nGrants immunity to knockback and nearly all debuffs, plus 80 defense" +
                "\nReduces Ranged, Magic and Summoner Damage by 300%. +70% mana cost" + 
                "\n+20% move speed, +10% melee damage.");
        }

        public override void SetDefaults() {
            base.SetDefaults();
            item.defense = 80;
            item.value = PriceByRarity.Red_10;
            item.rare = ItemRarityID.Red;
        }

        public override void UpdateEquip(Player player) {
            base.UpdateEquip(player);
            player.moveSpeed += 0.20f;
            player.meleeDamage += 0.1f;
            player.magicDamage -= 3f;
            player.rangedDamage -= 3f;
            player.minionDamage -= 3f;
            player.manaCost += 0.7f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("BeholderShield2"), 1);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"), 2);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 120000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();

        }
    }
}
