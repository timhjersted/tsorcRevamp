using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class BlueHerosHat : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blue Hero's hat");
            Tooltip.SetDefault("Worn by the hero himself!\nCan be upgraded eventually with 7 Souls of Sight and 40,000 Dark Souls\n+40 Mana");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 12;
            item.defense = 10;
            item.value = 2000;
            item.rare = ItemRarityID.Green;
        }

        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += 40;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<BlueHerosShirt>() && legs.type == ModContent.ItemType<BlueHerosPants>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.accFlipper = true;
            player.accDivingHelm = true;
            player.allDamage += 0.09f;
            player.rangedCrit += 9;
            player.meleeCrit += 9;
            player.magicCrit += 9;
            player.thrownCrit += 9;
            player.meleeSpeed += 0.09f;
            player.moveSpeed += 0.09f;
            player.manaCost -= 0.09f;
            player.ammoCost80 = true;

            if (player.wet)
            {
                player.lifeRegen += 3;
                player.detectCreature = true;
                player.moveSpeed *= 5f;
            }
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.HerosHat, 1);
            recipe.AddIngredient(ItemID.MythrilBar, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
