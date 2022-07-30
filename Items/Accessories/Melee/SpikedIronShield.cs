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
                             "\nReduces damage taken by 4% and gives thorn buff" +
                             "\nbut also reduces non-melee damage by 25% and movement speed by 5%" +
                             "\nCan be upgraded with an Obsidian Shield and 5000 Dark Souls");

        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.defense = 3;
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
        }

        public override void UpdateEquip(Player player)
        {

            player.thorns = 1f;
            player.endurance += 0.04f;
            player.moveSpeed *= 0.95f;
            player.GetDamage(DamageClass.Ranged) -= 0.25f;
            player.GetDamage(DamageClass.Magic) -= 0.25f;
            player.GetDamage(DamageClass.Summon) -= 0.25f;
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("IronShield").Type);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }

}
