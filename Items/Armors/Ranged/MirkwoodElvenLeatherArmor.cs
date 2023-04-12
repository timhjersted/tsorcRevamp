using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Body)]
    public class MirkwoodElvenLeatherArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Gifted with healing. High defense not necessary." +
                "\n25% chance not to consume ammo" +
                "\nSet Bonus: 20% Ranged Crit, +20 Ranged Dmg, +7 Life Regen"); */
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 7;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<MirkwoodElvenBlondeHairStyle>() && legs.type == ModContent.ItemType<MirkwoodElvenLeggings>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.2f;
            player.GetCritChance(DamageClass.Ranged) += 20;
            player.lifeRegen += 7;
        }
        public override void UpdateEquip(Player player)
        {
            player.ammoCost75 = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilChainmail, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
