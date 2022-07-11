using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientHornedHelmet : ModItem //To be reworked
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A treasure from ancient Plains of Havoc\nIncreases ranged crit by 12%");
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 5;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item); //TODO: Set this once it's reworked
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Ranged) += 12;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AncientMagicPlateArmor>() && legs.type == ModContent.ItemType<AncientMagicPlateGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.hasJumpOption_Sandstorm = true;
            player.GetDamage(DamageClass.Ranged) += 0.1f;
            player.ammoCost75 = true;
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FossilHelm, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 2200);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
