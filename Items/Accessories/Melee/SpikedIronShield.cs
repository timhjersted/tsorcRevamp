using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Melee
{

    [AutoloadEquip(EquipType.Shield)]

    public class SpikedIronShield : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("'Everyone will stay away from you'" +
                             "\nReduces damage taken by 7% and gives thorn buff" +
                             "\nbut also reduces non-melee damage by 25%" +
                             "\nCan be upgraded with an Obsidian Shield and 10000 Dark Souls");

        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.defense = 8;
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
        }

        public override void UpdateEquip(Player player)
        {

            player.thorns = 1f;
            player.endurance += 0.07f;
            player.GetDamage(DamageClass.Ranged) -= 0.25f;
            player.GetDamage(DamageClass.Magic) -= 0.25f;
            player.GetDamage(DamageClass.Summon) -= 0.25f;
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("IronShield").Type);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }

}
