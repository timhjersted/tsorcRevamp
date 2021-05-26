using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Head)]
    public class AncientGoldenHelmet2 : ModItem {
        public override string Texture => "tsorcRevamp/Items/Armors/AncientGoldenHelmet";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ancient Golden Helmet II");
            Tooltip.SetDefault("It is the famous Helmet of the Stars. \n9% melee speed\nSet bonus boosts all critical hits by 8%, +9% melee and ranged damage, +60 mana");
        }

        public override void SetDefaults() {
            item.width = 18;
            item.height = 18;
            item.defense = 5;
            item.value = 15000;
            item.rare = ItemRarityID.Green;
        }

        public override void UpdateEquip(Player player) {
            player.meleeSpeed += 0.09f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs) {
            return body.type == ModContent.ItemType<AncientGoldenArmor>() && legs.type == ModContent.ItemType<AncientGoldenGreaves>();
        }

        public override void UpdateArmorSet(Player player) {
            player.meleeDamage += 0.09f;
            player.rangedDamage += 0.09f;
            player.statManaMax2 += 60;
            player.rangedCrit += 8;
            player.magicCrit += 8;
            player.meleeCrit += 8;
            player.thrownCrit += 8; //lol
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<AncientGoldenHelmet>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
