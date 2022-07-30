using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Head)]
    public class BlackBeltHairStyle : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("You are a master of the zen arts, at one with the Tao\nAdds improved vision at night");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 2;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.nightVision = true;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<BlackBeltGiTop>() && legs.type == ModContent.ItemType<BlackBeltGiPants>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += 0.20f;
            player.GetDamage(DamageClass.Melee) += 0.20f;
            player.GetCritChance(DamageClass.Melee) += 7;
            if (player.statDefense >= 30)
            {
                player.statDefense = 30;
            }
            player.lifeRegen += 13;
            player.moveSpeed += player.endurance;
            player.endurance = 0f;
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilHelmet, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
