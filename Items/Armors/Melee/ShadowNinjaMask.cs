using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Head)]
    class ShadowNinjaMask : ModItem
    {

        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("+30% Melee Crit" +
                "\nSet bonus: +30% Melee Speed, +30 rapid life regen" +
                "\nDefense is capped at 40" +
                "\nDamage reduction is converted into movement speed"); */
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
            Item.defense = 5;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ShadowNinjaTop>() && legs.type == ModContent.ItemType<ShadowNinjaBottoms>();
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += 30;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += 0.3f;
            if (player.statDefense >= 40)
            {
                player.statDefense *= 0;
                player.statDefense += 40;
            }
            player.lifeRegen += 30;
            player.moveSpeed += player.endurance;
            player.endurance = 0f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BlackBeltHairStyle>());
            recipe.AddIngredient(ItemID.SoulofFright, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
