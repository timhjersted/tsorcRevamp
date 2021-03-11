using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class PowerArmorNUTorso : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("17% Increased Ranged & Magic Damage\n+80 mana, -50% mana cost\nA powerful armor forged by the god of chaos.\nSet Bonus: +17 Crit, 1/5 chance of healing 1/4th the amount of melee damage dealt, +10 life regen/20 in water");
            DisplayName.SetDefault("Power Armor NU Torso");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 7;
            item.value = 10000;
            item.rare = ItemRarityID.Pink;
        }

        public override void UpdateEquip(Player player)
        {
            player.rangedDamage += 0.17f;
            player.magicDamage += 0.17f;
            player.manaCost -= 0.50f;
            player.statManaMax2 += 80;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.ShadowScalemail, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 3);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
