using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Head)]
    class ShadowNinjaMask2 : ModItem {

        public override string Texture => "tsorcRevamp/Items/Armors/ShadowNinjaMask";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Shadow Ninja Armor II");
            Tooltip.SetDefault("Set bonus grants +30% to all damage, 100 mana, +30% Melee Speed, +30 Rapid Life Regen, +30% Crit" +
                "\n+12 special abilities of the Ninja." +
                "\nThese include: Firewalk, No fall damage, No knockback, rapid pick speed, waterwalk," +
                "\nreduced potion cooldown, double jump, jump boost, +30 % movement speed," +
                "\narchery, immunity to fire, and night vision." +
                "\nLife regen is dispelled if defense is higher than 60.");
        }
        public override void SetDefaults() {
            item.width = 18;
            item.height = 12;
            item.value = 50000;
            item.rare = ItemRarityID.Orange;
            item.defense = 5;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs) {
            return body.type == ModContent.ItemType<ShadowNinjaTop>() && legs.type == ModContent.ItemType<ShadowNinjaBottoms>();
        }

        public override void UpdateArmorSet(Player player) {
            player.allDamage += 0.3f;
            player.meleeSpeed += 0.3f;
            player.magicCrit += 30;
            player.meleeCrit += 30;
            player.rangedCrit += 30;
            player.thrownCrit += 30; //lol
            player.fireWalk = true;
            player.noFallDmg = true;
            player.noKnockback = true;
            player.pickSpeed += 0.2f;
            player.waterWalk = true;
            player.pStone = true;
            player.doubleJumpCloud = true;
            player.jumpBoost = true;
            player.moveSpeed += 0.3f;
            player.archery = true;
            player.fireWalk = true;
            player.nightVision = true;
            if (player.statDefense <= 60) {
                player.lifeRegen += 30;
            }
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<ShadowNinjaMask>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>());
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
