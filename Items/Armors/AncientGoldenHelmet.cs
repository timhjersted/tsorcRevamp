using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientGoldenHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("It is the famous Helmet of the Stars. \n7% melee speed\nSet bonus boosts all critical hits by 6%, +5% melee and ranged damage, +40 mana \nCan be upgraded with 4000 Dark Souls and 8 Stinger.");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 5;
            item.value = 15000;
            item.rare = ItemRarityID.Green;
        }

        public override void UpdateEquip(Player player)
        {
            player.meleeSpeed += 0.07f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AncientGoldenArmor>() && legs.type == ModContent.ItemType<AncientGoldenGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.meleeDamage += 0.05f;
            player.rangedDamage += 0.05f;
            player.statManaMax2 += 40;
            player.rangedCrit += 6;
            player.magicCrit += 6;
            player.meleeCrit += 6;
            player.thrownCrit += 6; //lol
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.GoldHelmet, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
