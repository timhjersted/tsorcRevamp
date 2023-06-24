using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Body)]
    public class OldChainArmor : ModItem
    {
        public static int FlatDmg = 1;
        public static int MinionSlots = 1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(FlatDmg, MinionSlots);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 3;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon).Flat += FlatDmg;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<OldChainCoif>() && legs.type == ModContent.ItemType<OldChainGreaves>(); 
        }

        public override void UpdateArmorSet(Player player)
        {
            player.maxMinions += MinionSlots;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverChainmail);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 300);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
